using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Posts;

public static class GetPublishedPost
{
    public sealed record Response(
        string Slug, string Title, string? Excerpt, string BodyMd, string? Category, DateTimeOffset PublishedAt, PhotoRef? Cover);

    public sealed record PhotoRef(string BaseUrl, int Width, int Height, string Alt);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{slug}", Handle).WithName(nameof(GetPublishedPost)).WithSummary("Uma notícia publicada");

    private static async Task<Results<Ok<Response>, NotFound>> Handle(
        string slug, AppDbContext db, IObjectStorage storage, TimeProvider clock, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var p = await db.Posts
            .Where(p => p.Slug == slug && p.Status == PostStatus.Published && p.PublishedAt <= now)
            .Select(p => new
            {
                p.Slug, p.Title, p.Excerpt, p.BodyMd,
                Category = p.Category != null ? p.Category.Name : null,
                PublishedAt = p.PublishedAt!.Value,
                Cover = p.CoverPhoto != null && p.CoverPhoto.Visibility == PhotoVisibility.Public
                    ? new { p.CoverPhoto.Id, Width = p.CoverPhoto.Width!.Value, Height = p.CoverPhoto.Height!.Value, Alt = p.CoverPhoto.AltText! }
                    : null,
            })
            .FirstOrDefaultAsync(ct);
        if (p is null) return TypedResults.NotFound();

        return TypedResults.Ok(new Response(p.Slug, p.Title, p.Excerpt, p.BodyMd, p.Category, p.PublishedAt,
            p.Cover is null ? null : new PhotoRef(storage.PhotoBaseUrl(p.Cover.Id), p.Cover.Width, p.Cover.Height, p.Cover.Alt)));
    }
}
