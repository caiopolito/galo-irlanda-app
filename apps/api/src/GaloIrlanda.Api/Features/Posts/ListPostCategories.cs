using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Posts;

public static class ListPostCategories
{
    public sealed record Item(short Id, string Slug, string Name);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/categories", Handle).WithName(nameof(ListPostCategories)).WithSummary("Categorias de notícia");

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.PostCategories
            .OrderBy(c => c.SortOrder)
            .Select(c => new Item(c.Id, c.Slug, c.Name))
            .ToListAsync(ct));
}
