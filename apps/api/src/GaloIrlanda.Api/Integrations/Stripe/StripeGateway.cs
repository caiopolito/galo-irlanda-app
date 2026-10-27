using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace GaloIrlanda.Api.Integrations.Stripe;

public sealed class StripeOptions
{
    public string? SecretKey { get; set; }
    public string? WebhookSecret { get; set; }
    /// <summary>Preço recorrente (mensalidade) criado no painel do Stripe: price_...</summary>
    public string? PriceId { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrEmpty(SecretKey) && !string.IsNullOrEmpty(WebhookSecret) && !string.IsNullOrEmpty(PriceId);
}

public sealed record CheckoutRequest(
    string Email, string SuccessUrl, string CancelUrl, IReadOnlyDictionary<string, string> Metadata);

public sealed record SubscriptionState(string Status, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd);

/// <summary>Chamadas ao Stripe usadas pela torcida. Interface para os testes não falarem com o Stripe.</summary>
public interface IStripeGateway
{
    Task<string> CreateCheckoutUrlAsync(CheckoutRequest request, CancellationToken ct);
    Task<SubscriptionState> GetSubscriptionAsync(string subscriptionId, CancellationToken ct);
}

public sealed class StripeGateway(IOptions<StripeOptions> options) : IStripeGateway
{
    private StripeClient Client => new(options.Value.SecretKey);

    public async Task<string> CreateCheckoutUrlAsync(CheckoutRequest request, CancellationToken ct)
    {
        var session = await new SessionService(Client).CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            CustomerEmail = request.Email,
            LineItems = [new SessionLineItemOptions { Price = options.Value.PriceId, Quantity = 1 }],
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            Metadata = request.Metadata.ToDictionary(),
            SubscriptionData = new SessionSubscriptionDataOptions { Metadata = request.Metadata.ToDictionary() },
            Locale = "pt-BR",
        }, cancellationToken: ct);
        return session.Url;
    }

    public async Task<SubscriptionState> GetSubscriptionAsync(string subscriptionId, CancellationToken ct)
    {
        var subscription = await new SubscriptionService(Client).GetAsync(subscriptionId, cancellationToken: ct);
        return ToState(subscription);
    }

    public static SubscriptionState ToState(global::Stripe.Subscription subscription)
    {
        // Desde a API 2025-03-31, o fim do período fica nos itens da assinatura.
        var periodEnd = subscription.Items?.Data.FirstOrDefault()?.CurrentPeriodEnd;
        return new SubscriptionState(
            subscription.Status,
            periodEnd is { } end ? new DateTimeOffset(DateTime.SpecifyKind(end, DateTimeKind.Utc)) : null,
            subscription.CancelAtPeriodEnd);
    }
}

public static class StripeSetup
{
    public static IServiceCollection AddStripe(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StripeOptions>(configuration.GetSection("Stripe"));
        services.AddSingleton<IStripeGateway, StripeGateway>();
        return services;
    }
}
