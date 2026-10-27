using System.Net;
using System.Net.Http.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Users;
using GaloIrlanda.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Tests.Features.Users;

public sealed class UsersTests(ApiFactory api) : IAsyncLifetime
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => api.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Admin_invites_an_editor()
    {
        var admin = await api.CreateClientAsAsync(Roles.Admin);

        var response = await admin.PostAsJsonAsync("/api/admin/users/invite",
            new InviteUser.Request("nova@teste.local", "Nova Voluntária", Roles.Editor), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nova@teste.local", Assert.Single(api.Email.Sent).To);
        await using var db = api.CreateDbContext();
        var user = await db.Users.Include(u => u.Roles).SingleAsync(u => u.Email == "nova@teste.local", Ct);
        Assert.Equal([Roles.Editor], user.Roles.Select(r => r.Role));
        var audit = await db.AuditEntries.Where(a => a.EntityId == user.Id.ToString()).Select(a => a.Action).ToListAsync(Ct);
        Assert.Equal(["invite", "role_grant"], audit.Order());
        Assert.DoesNotContain(await db.AuditEntries.Select(a => a.Changes).ToListAsync(Ct), c => c != null && c.Contains("@"));
    }

    [Fact]
    public async Task Editors_cannot_manage_users()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);

        var response = await editor.GetAsync("/api/admin/users", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task The_last_admin_cannot_lose_the_role()
    {
        var (admin, adminId) = await api.CreateClientWithUserAsync(Roles.Admin);

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{adminId}/roles",
            new ChangeUserRoles.Request([Roles.Editor]), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_disable_themselves_but_can_disable_others()
    {
        var (admin, adminId) = await api.CreateClientWithUserAsync(Roles.Admin);
        var (_, editorId) = await api.CreateClientWithUserAsync(Roles.Editor);

        var self = await admin.PostAsync($"/api/admin/users/{adminId}/disable", null, Ct);
        var other = await admin.PostAsync($"/api/admin/users/{editorId}/disable", null, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, self.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, other.StatusCode);
        await using var db = api.CreateDbContext();
        Assert.Equal(UserStatus.Disabled, (await db.Users.SingleAsync(u => u.Id == editorId, Ct)).Status);
    }
}
