using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GaloIrlanda.Api.Tests.Support;

/// <summary>Monta eventos de webhook do Stripe assinados como o Stripe assina (HMAC-SHA256 de "t.payload").</summary>
public static class StripeWebhooks
{
    public const string Secret = "whsec_teste";

    public static HttpRequestMessage Request(object stripeEvent, string secret = Secret)
    {
        var payload = JsonSerializer.Serialize(stripeEvent);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{payload}")));

        var request = new HttpRequestMessage(HttpMethod.Post, "/stripe/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={signature}");
        return request;
    }

    public static object CheckoutCompleted(
        string eventId, string email, string fullName, string subscriptionId, string? referralCode = null) => new
    {
        id = eventId,
        @object = "event",
        created = 1_793_000_000,
        type = "checkout.session.completed",
        data = new
        {
            @object = new
            {
                id = "cs_" + eventId,
                @object = "checkout.session",
                mode = "subscription",
                customer = "cus_" + subscriptionId,
                subscription = subscriptionId,
                customer_details = new { email, name = fullName },
                metadata = new Dictionary<string, string>
                {
                    ["full_name"] = fullName,
                    ["city_code"] = "DUB",
                    ["phone"] = "",
                    ["referral_code"] = referralCode ?? "",
                },
            },
        },
    };

    public static object SubscriptionUpdated(string eventId, string subscriptionId, string status, long created) => new
    {
        id = eventId,
        @object = "event",
        created,
        type = "customer.subscription.updated",
        data = new
        {
            @object = new
            {
                id = subscriptionId,
                @object = "subscription",
                status,
                cancel_at_period_end = false,
                items = new
                {
                    @object = "list",
                    data = new[] { new { id = "si_1", @object = "subscription_item", current_period_end = 1_798_000_000 } },
                },
            },
        },
    };
}
