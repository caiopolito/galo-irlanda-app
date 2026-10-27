using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Photos;

public static class GetPhoto
{
    public sealed record Response(
        Guid Id,
        string BaseUrl,
        int? Width,
        int? Height,
        string? AltText,
        string? Title,
        string? Caption,
        DateOnly? TakenOn,
        Guid? EventId,
        PhotoVisibility Visibility,
        ConsentBasis ConsentBasis,
        string? ConsentNote,
        DateTimeOffset? ConsentConfirmedAt,
        bool WallFeatured,
        int? WallSortOrder,
        uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", Handle).WithName(nameof(GetPhoto)).WithSummary("Detalhe de uma foto");

    private static async Task<Results<Ok<Response>, NotFound>> Handle(
        Guid id, AppDbContext db, IObjectStorage storage, CancellationToken ct)
    {
        var p = await db.Photos.FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, ct);
        if (p is null) return TypedResults.NotFound();

        return TypedResults.Ok(new Response(p.Id, storage.PhotoBaseUrl(p.Id), p.Width, p.Height, p.AltText, p.Title,
            p.Caption, p.TakenOn, p.EventId, p.Visibility, p.ConsentBasis, p.ConsentNote, p.ConsentConfirmedAt,
            p.WallFeatured, p.WallSortOrder, p.Version));
    }
}
