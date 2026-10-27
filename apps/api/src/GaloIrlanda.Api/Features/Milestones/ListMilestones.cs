using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Milestones;

public static class ListMilestones
{
    public sealed record Item(Guid Id, short Year, string Text, Guid? PhotoId, short SortOrder, bool IsPublished, uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListMilestones)).WithSummary("Lista os marcos da história");

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Milestones
            .OrderBy(m => m.Year).ThenBy(m => m.SortOrder)
            .Select(m => new Item(m.Id, m.Year, m.Text, m.PhotoId, m.SortOrder, m.IsPublished, m.Version))
            .ToListAsync(ct));
}
