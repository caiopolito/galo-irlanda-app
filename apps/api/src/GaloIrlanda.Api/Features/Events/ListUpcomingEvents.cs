using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Events;

/// <summary>Painel de partidas público: eventos publicados a partir de 3 h atrás (jogo em andamento ainda aparece).</summary>
public static class ListUpcomingEvents
{
    private const int Limit = 20;

    public sealed record Item(
        Guid Id,
        string Slug,
        EventKind Kind,
        string Title,
        DateTimeOffset StartsAt,
        string CityCode,
        string CityName,
        string Place,
        EventStatus Status,
        int? Capacity,
        int SpotsTaken);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListUpcomingEvents)).WithSummary("Próximos jogos e encontros");

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var since = clock.GetUtcNow().AddHours(-3);
        var items = await db.Events
            .Where(e => e.IsPublished && e.Status != EventStatus.Cancelled && e.StartsAt >= since)
            .OrderBy(e => e.StartsAt)
            .Take(Limit)
            .Select(e => new Item(e.Id, e.Slug, e.Kind, e.Title, e.StartsAt, e.CityCode, e.City.Name,
                e.Venue != null ? e.Venue.Name : e.VenueNote ?? "", e.Status, e.Capacity, e.SpotsTaken))
            .ToListAsync(ct);

        return TypedResults.Ok(items);
    }
}
