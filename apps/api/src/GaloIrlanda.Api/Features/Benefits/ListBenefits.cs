using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Benefits;

public static class ListBenefits
{
    public sealed record Item(
        Guid Id, string Headline, string Title, string? Description, Guid? VenueId, bool Highlight, short SortOrder,
        bool IsActive, uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListBenefits)).WithSummary("Lista os benefícios");

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Benefits.OrderBy(b => b.SortOrder)
            .Select(b => new Item(b.Id, b.Headline, b.Title, b.Description, b.VenueId, b.Highlight, b.SortOrder, b.IsActive, b.Version))
            .ToListAsync(ct));
}
