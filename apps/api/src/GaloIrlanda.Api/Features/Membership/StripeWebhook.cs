using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Stripe;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using CheckoutSession = Stripe.Checkout.Session;

namespace GaloIrlanda.Api.Features.Membership;

/// <summary>
/// Webhook do Stripe, fonte da verdade da mensalidade (ADR 0008). Verifica a assinatura, processa
/// cada evento uma única vez (tabela <c>stripe_events</c>) e ignora eventos mais velhos que o estado
/// atual. Responde 500 em caso de erro para o Stripe tentar de novo.
/// </summary>
public static class StripeWebhook
{
    public const string Path = "/stripe/webhook";

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost(Path, Handle).WithName(nameof(StripeWebhook)).ExcludeFromDescription().AllowAnonymous();

    private static async Task<Results<Ok, BadRequest, StatusCodeHttpResult>> Handle(
        HttpRequest request, AppDbContext db, IStripeGateway stripe, ProvisionMember provision,
        IOptions<StripeOptions> options, TimeProvider clock, ILogger<Program> log, CancellationToken ct)
    {
        var payload = await new StreamReader(request.Body).ReadToEndAsync(ct);
        global::Stripe.Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload, request.Headers["Stripe-Signature"], options.Value.WebhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return TypedResults.BadRequest();
        }

        var record = await db.StripeEvents.FirstOrDefaultAsync(e => e.Id == stripeEvent.Id, ct);
        if (record?.ProcessedAt is not null) return TypedResults.Ok();
        if (record is null)
        {
            record = new StripeEvent { Id = stripeEvent.Id, Type = stripeEvent.Type, ReceivedAt = clock.GetUtcNow() };
            db.StripeEvents.Add(record);
        }

        try
        {
            var eventAt = new DateTimeOffset(DateTime.SpecifyKind(stripeEvent.Created, DateTimeKind.Utc));
            switch (stripeEvent.Data.Object)
            {
                case CheckoutSession { Mode: "subscription" } session when stripeEvent.Type == EventTypes.CheckoutSessionCompleted:
                    await ProvisionAsync(session, eventAt, stripe, provision, ct);
                    break;
                case global::Stripe.Subscription subscription when stripeEvent.Type.StartsWith("customer.subscription."):
                    await UpdateSubscriptionAsync(db, subscription, eventAt, ct);
                    break;
            }

            record.ProcessedAt = clock.GetUtcNow();
            record.Error = null;
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Falha ao processar o evento {EventId} ({Type}) do Stripe", stripeEvent.Id, stripeEvent.Type);
            db.ChangeTracker.Clear();
            await db.StripeEvents
                .Where(e => e.Id == stripeEvent.Id)
                .ExecuteDeleteAsync(ct); // sem registro: a nova tentativa do Stripe processa de novo
            return TypedResults.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task ProvisionAsync(
        CheckoutSession session, DateTimeOffset eventAt, IStripeGateway stripe, ProvisionMember provision, CancellationToken ct)
    {
        var metadata = session.Metadata ?? new Dictionary<string, string>();
        var email = session.CustomerDetails?.Email ?? session.CustomerEmail
            ?? throw new InvalidOperationException("Checkout sem e-mail.");

        await provision.RunAsync(new ProvisionMember.Input(
            Email: email.Trim(),
            FullName: metadata.GetValueOrDefault(MembershipMetadata.FullName) ?? session.CustomerDetails?.Name ?? email,
            CityCode: metadata.GetValueOrDefault(MembershipMetadata.CityCode) ?? "DUB",
            Phone: metadata.GetValueOrDefault(MembershipMetadata.Phone),
            ReferralCode: metadata.GetValueOrDefault(MembershipMetadata.ReferralCode),
            StripeCustomerId: session.CustomerId,
            StripeSubscriptionId: session.SubscriptionId,
            Subscription: await stripe.GetSubscriptionAsync(session.SubscriptionId, ct),
            EventAt: eventAt), ct);
    }

    private static async Task UpdateSubscriptionAsync(
        AppDbContext db, global::Stripe.Subscription subscription, DateTimeOffset eventAt, CancellationToken ct)
    {
        var mirror = await db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscription.Id, ct);
        // Assinatura ainda desconhecida: o checkout.session.completed cria o espelho com o estado atual.
        if (mirror is null || eventAt < mirror.LastEventAt) return;

        var state = StripeGateway.ToState(subscription);
        mirror.Status = state.Status;
        mirror.CurrentPeriodEnd = state.CurrentPeriodEnd;
        mirror.CancelAtPeriodEnd = state.CancelAtPeriodEnd;
        mirror.LastEventAt = eventAt;
    }
}
