using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Auth;

/// <summary>Sai neste dispositivo ou em todos (revoga todas as sessões do usuário).</summary>
public static class Logout
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/logout", (Delegate)Handle)
            .WithName(nameof(Logout))
            .WithSummary("Sai neste dispositivo");

        group.MapPost("/logout-everywhere", HandleEverywhere)
            .WithName("LogoutEverywhere")
            .WithSummary("Sai em todos os dispositivos")
            .RequireAuthorization();
    }

    private static async Task<NoContent> Handle(HttpContext http)
    {
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }

    private static async Task<NoContent> HandleEverywhere(
        HttpContext http, CurrentUser currentUser, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        await db.Sessions
            .Where(s => s.UserId == currentUser.Id && s.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now), ct);

        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }
}
