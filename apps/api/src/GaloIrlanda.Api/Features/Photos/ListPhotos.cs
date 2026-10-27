using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Photos;

/// <summary>Galeria do admin, das mais recentes para as mais antigas. <c>wall=true</c> mostra só o mural, na ordem dele.</summary>
public static class ListPhotos
{
    public sealed record Item(
        Guid Id,
        string BaseUrl,
        int? Width,
        int? Height,
        string? Title,
        PhotoVisibility Visibility,
        ConsentBasis ConsentBasis,
        bool WallFeatured);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListPhotos)).WithSummary("Lista fotos no admin");

    private static async Task<Ok<PagedResponse<Item>>> Handle(
        [AsParameters] PageRequest page, AppDbContext db, IObjectStorage storage, CancellationToken ct, bool wall = false)
    {
        var query = db.Photos.Where(p => p.DeletedAt == null && p.UploadStatus == PhotoUploadStatus.Ready);
        query = wall
            ? query.Where(p => p.WallFeatured).OrderBy(p => p.WallSortOrder)
            : query.OrderByDescending(p => p.CreatedAt);

        var result = await query
            .Select(p => new Item(p.Id, "", p.Width, p.Height, p.Title, p.Visibility, p.ConsentBasis, p.WallFeatured))
            .ToPagedAsync(page, ct);

        // A URL depende da configuração de storage, então é montada fora da consulta.
        var items = result.Items.Select(i => i with { BaseUrl = storage.PhotoBaseUrl(i.Id) }).ToList();
        return TypedResults.Ok(result with { Items = items });
    }
}
