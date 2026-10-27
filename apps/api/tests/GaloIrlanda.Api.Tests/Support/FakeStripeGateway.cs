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

    public BillingSummary Billing { get; set; } = new("4242", []);

    public Task<BillingSummary> GetBillingAsync(string customerId, string subscriptionId, CancellationToken ct) =>
        Task.FromResult(Billing);

    public Task<string> CreatePortalUrlAsync(string customerId, string returnUrl, CancellationToken ct) =>
        Task.FromResult($"https://billing.stripe.test/{customerId}");

    public void Reset()
    {
        Checkouts.Clear();
        Subscriptions.Clear();
        Billing = new("4242", []);
    }
}
