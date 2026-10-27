using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Auth;

/// <summary>Quem está logado. O front usa para decidir o que mostrar.</summary>
public static class GetMe
{
    public sealed record Response(Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/me", Handle)
            .WithName(nameof(GetMe))
            .WithSummary("Usuário logado")
            .RequireAuthorization();

    private static async Task<Results<Ok<Response>, UnauthorizedHttpResult>> Handle(
        CurrentUser currentUser, AppDbContext db, CancellationToken ct)
    {
        var response = await db.Users
            .Where(u => u.Id == currentUser.Id)
            .Select(u => new Response(u.Id, u.Email, u.DisplayName, u.Roles.Select(r => r.Role).ToList()))
            .FirstOrDefaultAsync(ct);

        return response is null ? TypedResults.Unauthorized() : TypedResults.Ok(response);
    }
}
