using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Posts;

/// <summary>Notícias no site: publicadas e com data já passada (agendadas ficam de fora até a hora).</summary>
public static class ListPublishedPosts
{
    public sealed record Item(
        string Slug, string Title, string? Excerpt, string? Category, DateTimeOffset PublishedAt, PhotoRef? Cover);

    public sealed record PhotoRef(string BaseUrl, int Width, int Height, string Alt);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListPublishedPosts)).WithSummary("Notícias publicadas");

    private static async Task<Ok<PagedResponse<Item>>> Handle(
        [AsParameters] PageRequest page, AppDbContext db, IObjectStorage storage, TimeProvider clock, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var result = await db.Posts
            .Where(p => p.Status == PostStatus.Published && p.PublishedAt <= now)
            .OrderByDescending(p => p.PublishedAt)
            .Select(p => new
            {
                p.Slug, p.Title, p.Excerpt,
                Category = p.Category != null ? p.Category.Name : null,
                PublishedAt = p.PublishedAt!.Value,
                Cover = p.CoverPhoto != null && p.CoverPhoto.Visibility == PhotoVisibility.Public
                    ? new { p.CoverPhoto.Id, Width = p.CoverPhoto.Width!.Value, Height = p.CoverPhoto.Height!.Value, Alt = p.CoverPhoto.AltText! }
                    : null,
            })
            .ToPagedAsync(page, ct);

        var items = result.Items.Select(p => new Item(p.Slug, p.Title, p.Excerpt, p.Category, p.PublishedAt,
            p.Cover is null ? null : new PhotoRef(storage.PhotoBaseUrl(p.Cover.Id), p.Cover.Width, p.Cover.Height, p.Cover.Alt)))
            .ToList();
        return TypedResults.Ok(new PagedResponse<Item>(items, result.Page, result.PageSize, result.Total));
    }
}
