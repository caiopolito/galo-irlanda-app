using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Integrations.Stripe;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>Mensalidade: valor, situação, próxima cobrança, cartão e recibos (lidos na hora do Stripe).</summary>
public static class GetBilling
{
    public sealed record Invoice(DateTimeOffset Date, long AmountCents, string Currency, string? ReceiptUrl);

    public sealed record Response(
        int? FeeCents, string Currency, MembershipState Membership, string? CardLast4, IReadOnlyList<Invoice> Invoices);

    public sealed record PortalResponse(string Url);

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/billing", Handle).WithName(nameof(GetBilling)).WithSummary("Mensalidade e recibos");
        group.MapPost("/billing/portal", Portal).WithName("OpenBillingPortal")
            .WithSummary("Abre o portal do Stripe (cartão, recibos, cancelamento)");
    }

    private static async Task<Ok<Response>> Handle(
        CurrentUser currentUser, AppDbContext db, IStripeGateway stripe, IOptions<StripeOptions> options, CancellationToken ct)
    {
        var settings = await db.SiteSettings.SingleAsync(ct);
        var subscription = await MemberStatus.LatestSubscriptionAsync(db, currentUser.Id!.Value, ct);
        var state = subscription is null
            ? MembershipState.None
            : new MembershipState(subscription.Status, subscription.IsActive, subscription.CurrentPeriodEnd, subscription.CancelAtPeriodEnd);

        var billing = subscription is not null && options.Value.IsConfigured
            ? await stripe.GetBillingAsync(subscription.StripeCustomerId, subscription.StripeSubscriptionId, ct)
            : new BillingSummary(null, []);

        return TypedResults.Ok(new Response(settings.MembershipFeeCents, settings.MembershipCurrency, state, billing.CardLast4,
            billing.Invoices.Select(i => new Invoice(i.Date, i.AmountCents, i.Currency, i.ReceiptUrl)).ToList()));
    }

    private static async Task<Results<Ok<PortalResponse>, NotFound>> Portal(
        CurrentUser currentUser, AppDbContext db, IStripeGateway stripe, IConfiguration configuration, CancellationToken ct)
    {
        var subscription = await MemberStatus.LatestSubscriptionAsync(db, currentUser.Id!.Value, ct);
        if (subscription is null) return TypedResults.NotFound();

        var url = await stripe.CreatePortalUrlAsync(subscription.StripeCustomerId, $"{configuration["Site:PublicUrl"]}/socio", ct);
        return TypedResults.Ok(new PortalResponse(url));
    }
}
