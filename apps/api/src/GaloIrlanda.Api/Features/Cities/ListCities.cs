using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Cities;

public static class ListCities
{
    public sealed record Item(string Code, string Name, string? Blurb, Guid? PhotoId, short SortOrder, bool IsActive, uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListCities)).WithSummary("Lista as cidades");

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Cities
            .OrderBy(c => c.SortOrder)
            .Select(c => new Item(c.Code, c.Name, c.Blurb, c.PhotoId, c.SortOrder, c.IsActive, c.Version))
            .ToListAsync(ct));
}
