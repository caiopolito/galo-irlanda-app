using System.Net;
using System.Net.Http.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Auth;
using GaloIrlanda.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Tests.Features.Auth;

public sealed class MagicLinkTests(ApiFactory api) : IAsyncLifetime
{
    private const string EditorEmail = "editora@teste.local";
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await api.ResetAsync();
        var user = new User { Email = EditorEmail, DisplayName = "Editora" };
        user.Roles.Add(new UserRole { UserId = user.Id, Role = Roles.Editor });
        await api.SeedAsync(user);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<HttpResponseMessage> RequestLink(HttpClient client, string email) =>
        await client.PostAsJsonAsync("/api/auth/magic-link", new RequestMagicLink.Request(email), Ct);

    private async Task<HttpResponseMessage> Consume(HttpClient client, string token) =>
        await client.PostAsJsonAsync("/api/auth/magic-link/consume", new ConsumeMagicLink.Request(token), Ct);

    private async Task<HttpClient> LoggedInBrowser()
    {
        var browser = api.CreateBrowserClient();
        await RequestLink(browser, EditorEmail);
        var response = await Consume(browser, api.Email.LastTokenFor(EditorEmail));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return browser;
    }

    [Fact]
    public async Task Full_flow_link_then_cookie_session_then_logout()
    {
        var browser = api.CreateBrowserClient();

        var requested = await RequestLink(browser, "EDITORA@teste.local");
        Assert.Equal(HttpStatusCode.OK, requested.StatusCode);
        var email = Assert.Single(api.Email.Sent);
        Assert.Equal(EditorEmail, email.To);
        Assert.Contains("https://site.teste/entrar?token=", email.Text);

        var consumed = await Consume(browser, api.Email.LastTokenFor(EditorEmail));
        Assert.Equal(HttpStatusCode.OK, consumed.StatusCode);
        Assert.Contains(consumed.Headers.GetValues("Set-Cookie"), c => c.StartsWith("galo_session=") && c.Contains("httponly"));

        var me = await browser.GetFromJsonAsync<GetMe.Response>("/api/auth/me", Ct);
        Assert.Equal(EditorEmail, me!.Email);
        Assert.Equal([Roles.Editor], me.Roles);

        Assert.Equal(HttpStatusCode.NoContent, (await browser.PostAsync("/api/auth/logout", null, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await browser.GetAsync("/api/auth/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Session_cookie_gives_access_to_the_admin()
    {
        var browser = await LoggedInBrowser();

        var response = await browser.GetAsync("/api/admin/events", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_email_gets_the_same_answer_and_no_email()
    {
        var response = await RequestLink(api.CreateBrowserClient(), "ninguem@teste.local");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(api.Email.Sent);
    }

    [Fact]
    public async Task Link_works_only_once()
    {
        await RequestLink(api.CreateBrowserClient(), EditorEmail);
        var token = api.Email.LastTokenFor(EditorEmail);

        var first = await Consume(api.CreateBrowserClient(), token);
        var second = await Consume(api.CreateBrowserClient(), token);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Link_expires_after_15_minutes()
    {
        await RequestLink(api.CreateBrowserClient(), EditorEmail);
        api.Clock.Advance(TimeSpan.FromMinutes(16));

        var response = await Consume(api.CreateBrowserClient(), api.Email.LastTokenFor(EditorEmail));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task At_most_three_links_per_15_minutes()
    {
        var browser = api.CreateBrowserClient();
        for (var i = 0; i < 5; i++) await RequestLink(browser, EditorEmail);

        Assert.Equal(3, api.Email.Sent.Count);
        await using var db = api.CreateDbContext();
        Assert.All(await db.LoginTokens.ToListAsync(Ct), t => Assert.Equal(32, t.TokenHash.Length));
    }

    [Fact]
    public async Task Disabling_a_user_ends_their_sessions_immediately()
    {
        var browser = await LoggedInBrowser();
        await using (var db = api.CreateDbContext())
        {
            var user = await db.Users.SingleAsync(u => u.Email == EditorEmail, Ct);
            user.Status = UserStatus.Disabled;
            await db.SaveChangesAsync(Ct);
        }

        var response = await browser.GetAsync("/api/auth/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Role_changes_apply_to_existing_sessions()
    {
        var browser = await LoggedInBrowser();
        await using (var db = api.CreateDbContext())
        {
            await db.UserRoles.Where(r => r.Role == Roles.Editor).ExecuteDeleteAsync(Ct);
        }

        var response = await browser.GetAsync("/api/admin/events", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Logout_everywhere_revokes_other_devices()
    {
        var phone = await LoggedInBrowser();
        api.Clock.Advance(TimeSpan.FromSeconds(1));
        var laptop = await LoggedInBrowser();

        var response = await laptop.PostAsync("/api/auth/logout-everywhere", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await phone.GetAsync("/api/auth/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Session_is_stored_hashed_with_expiry()
    {
        await LoggedInBrowser();

        await using var db = api.CreateDbContext();
        var session = await db.Sessions.SingleAsync(Ct);
        Assert.Equal(32, session.TokenHash.Length);
        Assert.Equal(session.CreatedAt + SessionTicketStore.SlidingExpiration, session.ExpiresAt);
        Assert.Null(session.RevokedAt);
    }
}
