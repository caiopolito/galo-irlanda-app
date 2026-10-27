using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>"Fotos em que você aparece" e a opção de sair de uma foto (GDPR).</summary>
public static class MyPhotos
{
    public sealed record Item(Guid Id, string BaseUrl, int Width, int Height, string? AltText, string? Title);

    public sealed record Response(int Total, IReadOnlyList<Item> Photos);

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/photos", Handle).WithName("ListMyPhotos").WithSummary("Fotos em que o sócio aparece");
        group.MapDelete("/photos/{photoId:guid}/tag", Untag).WithName("RemoveMyPhotoTag").WithSummary("Sai de uma foto");
    }

    private static async Task<Ok<Response>> Handle(
        CurrentUser currentUser, AppDbContext db, IObjectStorage storage, CancellationToken ct, int take = 12)
    {
        var userId = currentUser.Id!.Value;
        var query = db.PhotoTags.Where(t => t.UserId == userId)
            .Join(db.Photos.Where(p => p.DeletedAt == null && p.UploadStatus == PhotoUploadStatus.Ready &&
                                       p.Visibility != PhotoVisibility.Private),
                t => t.PhotoId, p => p.Id, (t, p) => p);

        var total = await query.CountAsync(ct);
        var photos = await query.OrderByDescending(p => p.CreatedAt).Take(Math.Clamp(take, 1, 100))
            .Select(p => new { p.Id, p.Width, p.Height, p.AltText, p.Title }).ToListAsync(ct);

        return TypedResults.Ok(new Response(total, photos
            .Select(p => new Item(p.Id, storage.PhotoBaseUrl(p.Id), p.Width ?? 0, p.Height ?? 0, p.AltText, p.Title)).ToList()));
    }

    private static async Task<NoContent> Untag(Guid photoId, CurrentUser currentUser, AppDbContext db, CancellationToken ct)
    {
        await db.PhotoTags.Where(t => t.PhotoId == photoId && t.UserId == currentUser.Id).ExecuteDeleteAsync(ct);
        return TypedResults.NoContent();
    }
}
