using GaloIrlanda.Api.Integrations.Stripe;

namespace GaloIrlanda.Api.Tests.Support;

public sealed class FakeStripeGateway : IStripeGateway
{
    public List<CheckoutRequest> Checkouts { get; } = [];
    public Dictionary<string, SubscriptionState> Subscriptions { get; } = [];

    public Task<string> CreateCheckoutUrlAsync(CheckoutRequest request, CancellationToken ct)
    {
        Checkouts.Add(request);
        return Task.FromResult($"https://checkout.stripe.test/{Checkouts.Count}");
    }

    public Task<SubscriptionState> GetSubscriptionAsync(string subscriptionId, CancellationToken ct) =>
        Task.FromResult(Subscriptions.GetValueOrDefault(subscriptionId)
            ?? new SubscriptionState("active", new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero), false));

    public void Reset()
    {
        Checkouts.Clear();
        Subscriptions.Clear();
    }
}
