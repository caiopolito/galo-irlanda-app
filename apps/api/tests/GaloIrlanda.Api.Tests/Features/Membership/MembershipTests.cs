using System.Net;
using System.Net.Http.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Membership;
using GaloIrlanda.Api.Integrations.Stripe;
using GaloIrlanda.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Tests.Features.Membership;

public sealed class MembershipTests(ApiFactory api) : IAsyncLifetime
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await api.ResetAsync();
        await api.SeedAsync(new City { Code = "DUB", Name = "Dublin" });
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private Task<HttpResponseMessage> Send(object stripeEvent, string secret = StripeWebhooks.Secret) =>
        api.CreateClient().SendAsync(StripeWebhooks.Request(stripeEvent, secret), Ct);

    [Fact]
    public async Task Checkout_carries_the_signup_data_to_stripe()
    {
        var response = await (await api.CreateClientAsAsync(role: null)).PostAsJsonAsync("/api/public/membership/checkout",
            new StartCheckout.Request("Maria da Silva", "maria@teste.local", "dub", null, "abc123"), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StartCheckout.Response>(Json.Options, Ct);
        Assert.StartsWith("https://checkout.stripe.test/", body!.CheckoutUrl);
        var checkout = Assert.Single(api.Stripe.Checkouts);
        Assert.Equal("maria@teste.local", checkout.Email);
        Assert.Equal("DUB", checkout.Metadata["city_code"]);
        Assert.Equal("ABC123", checkout.Metadata["referral_code"]);
        await using var db = api.CreateDbContext();
        Assert.False(await db.Users.AnyAsync(Ct)); // nada é gravado antes de pagar
    }

    [Fact]
    public async Task Paid_checkout_turns_the_person_into_a_member_once()
    {
        var paid = StripeWebhooks.CheckoutCompleted("evt_1", "maria@teste.local", "Maria da Silva", "sub_1");

        Assert.Equal(HttpStatusCode.OK, (await Send(paid)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Send(paid)).StatusCode); // o Stripe reenvia

        await using var db = api.CreateDbContext();
        var user = await db.Users.Include(u => u.Roles).SingleAsync(Ct);
        Assert.Equal("Maria", user.DisplayName);
        Assert.Equal([Roles.Socio], user.Roles.Select(r => r.Role));
        var member = await db.Members.SingleAsync(Ct);
        Assert.Equal("Maria da Silva", member.FullName);
        Assert.True(member.MemberNumber > 0);
        Assert.Equal(8, member.ReferralCode.Length);
        var subscription = await db.Subscriptions.SingleAsync(Ct);
        Assert.Equal("active", subscription.Status);
        Assert.Equal(new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero), subscription.CurrentPeriodEnd);
        var welcome = Assert.Single(api.Email.Sent);
        Assert.Contains($"sócio nº {member.MemberNumber:0000}", welcome.Subject);
    }

    [Fact]
    public async Task Member_numbers_are_sequential_and_referrals_are_linked()
    {
        await Send(StripeWebhooks.CheckoutCompleted("evt_a", "a@teste.local", "Ana", "sub_a"));
        await using var db = api.CreateDbContext();
        var first = await db.Members.SingleAsync(Ct);

        await Send(StripeWebhooks.CheckoutCompleted("evt_b", "b@teste.local", "Bia", "sub_b", referralCode: first.ReferralCode));

        var second = await db.Members.AsNoTracking().SingleAsync(m => m.UserId != first.UserId, Ct);
        Assert.Equal(first.MemberNumber + 1, second.MemberNumber);
        Assert.Equal(first.UserId, second.ReferredBy);
    }

    [Fact]
    public async Task Existing_member_cannot_start_another_checkout()
    {
        await Send(StripeWebhooks.CheckoutCompleted("evt_1", "maria@teste.local", "Maria", "sub_1"));

        var response = await (await api.CreateClientAsAsync(role: null)).PostAsJsonAsync("/api/public/membership/checkout",
            new StartCheckout.Request("Maria", "MARIA@teste.local", "DUB", null, null), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_with_a_wrong_signature_is_rejected()
    {
        var response = await Send(StripeWebhooks.CheckoutCompleted("evt_1", "x@teste.local", "X", "sub_1"), secret: "whsec_errado");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var db = api.CreateDbContext();
        Assert.False(await db.Members.AnyAsync(Ct));
    }

    [Fact]
    public async Task Subscription_updates_apply_in_order()
    {
        await Send(StripeWebhooks.CheckoutCompleted("evt_1", "maria@teste.local", "Maria", "sub_1"));

        await Send(StripeWebhooks.SubscriptionUpdated("evt_2", "sub_1", "past_due", created: 1_793_000_100));
        await Send(StripeWebhooks.SubscriptionUpdated("evt_old", "sub_1", "active", created: 1_793_000_050));

        await using var db = api.CreateDbContext();
        var subscription = await db.Subscriptions.SingleAsync(Ct);
        Assert.Equal("past_due", subscription.Status);
        Assert.False(subscription.IsActive);
    }

    [Fact]
    public async Task Payment_data_comes_from_stripe_not_from_the_event_payload()
    {
        api.Stripe.Subscriptions["sub_1"] = new SubscriptionState("trialing", null, CancelAtPeriodEnd: true);

        await Send(StripeWebhooks.CheckoutCompleted("evt_1", "maria@teste.local", "Maria", "sub_1"));

        await using var db = api.CreateDbContext();
        var subscription = await db.Subscriptions.SingleAsync(Ct);
        Assert.Equal("trialing", subscription.Status);
        Assert.True(subscription.CancelAtPeriodEnd);
    }
}
