using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>"Meus embarques": próximos eventos publicados com a resposta do sócio, e o próximo jogo em destaque.</summary>
public static class ListMemberEvents
{
    public sealed record Item(
        Guid Id,
        string Slug,
        EventKind Kind,
        string Title,
        DateTimeOffset StartsAt,
        string Place,
        string CityName,
        EventStatus Status,
        RsvpAnswer? MyAnswer,
        int Going);

    public sealed record Response(Item? Next, IReadOnlyList<Item> Events);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/events", Handle).WithName(nameof(ListMemberEvents)).WithSummary("Meus embarques");

    private static async Task<Ok<Response>> Handle(
        CurrentUser currentUser, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var userId = currentUser.Id!.Value;
        var since = clock.GetUtcNow().AddHours(-3);

        var events = await db.Events
            .Where(e => e.IsPublished && e.Status != EventStatus.Cancelled && e.StartsAt >= since)
            .OrderBy(e => e.StartsAt)
            .Take(20)
            .Select(e => new Item(
                e.Id, e.Slug, e.Kind, e.Title, e.StartsAt,
                e.Venue != null
                    ? (e.Venue.Neighborhood != null ? e.Venue.Name + ", " + e.Venue.Neighborhood : e.Venue.Name)
                    : e.VenueNote ?? "",
                e.City.Name,
                e.Status,
                db.EventRsvps.Where(r => r.EventId == e.Id && r.UserId == userId).Select(r => (RsvpAnswer?)r.Answer).FirstOrDefault(),
                e.SpotsTaken + db.EventRsvps.Count(r => r.EventId == e.Id && r.Answer == RsvpAnswer.Going)))
            .ToListAsync(ct);

        var next = events.FirstOrDefault(e => e.Kind == EventKind.Match) ?? events.FirstOrDefault();
        return TypedResults.Ok(new Response(next, events));
    }
}
