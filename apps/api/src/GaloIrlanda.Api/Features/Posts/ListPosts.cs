using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Posts;

/// <summary>Lista do admin com rascunhos, agendadas e arquivadas.</summary>
public static class ListPosts
{
    public sealed record Item(
        Guid Id, string Title, string? Category, PostStatus Status, DateTimeOffset? PublishedAt, DateTimeOffset UpdatedAt);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListPosts)).WithSummary("Lista notícias no admin");

    private static async Task<Ok<PagedResponse<Item>>> Handle(
        [AsParameters] PageRequest page, AppDbContext db, CancellationToken ct, PostStatus? status = null)
    {
        var query = db.Posts.AsQueryable();
        if (status is not null) query = query.Where(p => p.Status == status);

        var result = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new Item(p.Id, p.Title, p.Category != null ? p.Category.Name : null, p.Status, p.PublishedAt, p.UpdatedAt))
            .ToPagedAsync(page, ct);

        return TypedResults.Ok(result);
    }
}
