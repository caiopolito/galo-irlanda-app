using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Posts;

public static class GetPost
{
    public sealed record Response(
        Guid Id,
        string Slug,
        string Title,
        string? Excerpt,
        string BodyMd,
        short? CategoryId,
        Guid? CoverPhotoId,
        PostStatus Status,
        DateTimeOffset? PublishedAt,
        DateTimeOffset UpdatedAt,
        uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", Handle).WithName(nameof(GetPost)).WithSummary("Detalhe de uma notícia");

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext db, CancellationToken ct)
    {
        var response = await db.Posts
            .Where(p => p.Id == id)
            .Select(p => new Response(p.Id, p.Slug, p.Title, p.Excerpt, p.BodyMd, p.CategoryId, p.CoverPhotoId,
                p.Status, p.PublishedAt, p.UpdatedAt, p.Version))
            .FirstOrDefaultAsync(ct);

        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    }
}
