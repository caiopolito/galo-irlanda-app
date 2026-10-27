using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Events;

/// <summary>Lista do admin com rascunhos. <c>scope</c>: <c>upcoming</c> (padrão), <c>past</c> ou <c>all</c>.</summary>
public static class ListEvents
{
    public sealed record Item(
        Guid Id,
        EventKind Kind,
        string Title,
        DateTimeOffset StartsAt,
        string CityCode,
        string Place,
        EventStatus Status,
        bool IsPublished);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListEvents)).WithSummary("Lista eventos no admin");

    private static async Task<Ok<PagedResponse<Item>>> Handle(
        [AsParameters] PageRequest page, AppDbContext db, TimeProvider clock, CancellationToken ct, string scope = "upcoming")
    {
        var now = clock.GetUtcNow();
        var query = db.Events.AsQueryable();
        query = scope switch
        {
            // Texto, e não enum: o bind de enum na query diferencia maiúsculas de minúsculas.
            "past" => query.Where(e => e.StartsAt < now.AddHours(-3)).OrderByDescending(e => e.StartsAt),
            "all" => query.OrderByDescending(e => e.StartsAt),
            _ => query.Where(e => e.StartsAt >= now.AddHours(-3)).OrderBy(e => e.StartsAt),
        };

        var result = await query
            .Select(e => new Item(e.Id, e.Kind, e.Title, e.StartsAt, e.CityCode,
                e.Venue != null ? e.Venue.Name : e.VenueNote ?? "", e.Status, e.IsPublished))
            .ToPagedAsync(page, ct);

        return TypedResults.Ok(result);
    }
}
