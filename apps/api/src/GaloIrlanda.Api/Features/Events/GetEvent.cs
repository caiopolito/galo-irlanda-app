using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Events;

/// <summary>Evento completo para o formulário de edição do admin.</summary>
public static class GetEvent
{
    public sealed record Response(
        Guid Id,
        string Slug,
        EventKind Kind,
        string Title,
        DateTimeOffset StartsAt,
        string CityCode,
        Guid? VenueId,
        string? VenueNote,
        string? Opponent,
        string? Competition,
        string? HomeTrip,
        string? DescriptionMd,
        Guid? CoverPhotoId,
        EventStatus Status,
        bool IsPublished,
        int? Capacity,
        int SpotsTaken,
        DateTimeOffset UpdatedAt,
        uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/{id:guid}", Handle).WithName(nameof(GetEvent)).WithSummary("Detalhe de um evento");

    private static async Task<Results<Ok<Response>, NotFound>> Handle(Guid id, AppDbContext db, CancellationToken ct)
    {
        var response = await db.Events
            .Where(e => e.Id == id)
            .Select(e => new Response(e.Id, e.Slug, e.Kind, e.Title, e.StartsAt, e.CityCode, e.VenueId, e.VenueNote,
                e.Opponent, e.Competition, e.HomeTrip, e.DescriptionMd, e.CoverPhotoId, e.Status, e.IsPublished,
                e.Capacity, e.SpotsTaken, e.UpdatedAt, e.Version))
            .FirstOrDefaultAsync(ct);

        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
    }
}
