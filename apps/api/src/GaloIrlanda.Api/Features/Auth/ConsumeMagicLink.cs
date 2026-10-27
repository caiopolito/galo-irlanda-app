using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Auth;

/// <summary>
/// Troca o token do link pelo cookie de sessão. É um POST chamado pela página /entrar do front:
/// consumir em GET deixaria scanners de e-mail queimarem o link.
/// </summary>
public static class ConsumeMagicLink
{
    private const string InvalidMessage = "Link inválido ou expirado. Peça um novo.";

    public sealed record Request(string Token);

    public sealed record Response(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Token).NotEmpty().MaximumLength(100);
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/magic-link/consume", Handle)
            .WithName(nameof(ConsumeMagicLink))
            .WithSummary("Entra com o token do link mágico")
            .WithValidation<Request>()
            .RequireRateLimiting(AuthModule.RateLimitPolicy);

    private static async Task<Results<Ok<Response>, ValidationProblem>> Handle(
        Request request, AppDbContext db, HttpContext http, TimeProvider clock, CancellationToken ct)
    {
        if (!LoginTokenCodec.TryHash(request.Token, out var hash)) return Invalid();

        var now = clock.GetUtcNow();
        var valid = db.LoginTokens.Where(t => t.TokenHash == hash && t.ConsumedAt == null && t.ExpiresAt > now);

        var userId = await valid.Select(t => (Guid?)t.UserId).FirstOrDefaultAsync(ct);
        // Consumo atômico: se duas abas usarem o mesmo link ao mesmo tempo, só uma entra.
        if (userId is null || await valid.ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, now), ct) == 0)
            return Invalid();

        var user = await db.Users.Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId && u.Status == UserStatus.Active, ct);
        if (user is null) return Invalid();

        user.LastLoginAt = now;
        await db.SaveChangesAsync(ct);

        var roles = user.Roles.Select(r => r.Role).ToList();
        await http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            UserPrincipal.Create(user.Id, user.DisplayName, roles),
            new AuthenticationProperties { IsPersistent = true });

        return TypedResults.Ok(new Response(user.Id, user.Email, user.DisplayName, roles));
    }

    private static ValidationProblem Invalid() =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["token"] = [InvalidMessage] });
}
