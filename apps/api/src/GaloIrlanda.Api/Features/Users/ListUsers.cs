using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Users;

public static class ListUsers
{
    public sealed record Item(
        Guid Id, string Email, string DisplayName, UserStatus Status, IReadOnlyList<string> Roles, DateTimeOffset? LastLoginAt);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListUsers)).WithSummary("Lista os usuários do admin");

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Users
            .Where(u => u.Status != UserStatus.Erased)
            .OrderBy(u => u.DisplayName)
            .Select(u => new Item(u.Id, u.Email, u.DisplayName, u.Status, u.Roles.Select(r => r.Role).ToList(), u.LastLoginAt))
            .ToListAsync(ct));
}
