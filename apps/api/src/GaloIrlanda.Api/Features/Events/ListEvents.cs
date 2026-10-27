using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Events;

/// <summary>Lista do admin: próximos (padrão), passados ou todos, com rascunhos.</summary>
public static class ListEvents
{
    public enum Scope { Upcoming, Past, All }

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
        [AsParameters] PageRequest page, AppDbContext db, TimeProvider clock, CancellationToken ct, Scope scope = Scope.Upcoming)
    {
        var now = clock.GetUtcNow();
        var query = db.Events.AsQueryable();
        query = scope switch
        {
            Scope.Upcoming => query.Where(e => e.StartsAt >= now.AddHours(-3)).OrderBy(e => e.StartsAt),
            Scope.Past => query.Where(e => e.StartsAt < now.AddHours(-3)).OrderByDescending(e => e.StartsAt),
            _ => query.OrderByDescending(e => e.StartsAt),
        };

        var result = await query
            .Select(e => new Item(e.Id, e.Kind, e.Title, e.StartsAt, e.CityCode,
                e.Venue != null ? e.Venue.Name : e.VenueNote ?? "", e.Status, e.IsPublished))
            .ToPagedAsync(page, ct);

        return TypedResults.Ok(result);
    }
}
