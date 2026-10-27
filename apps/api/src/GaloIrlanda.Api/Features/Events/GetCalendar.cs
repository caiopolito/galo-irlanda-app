using System.Text;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Events;

/// <summary>
/// Agenda em iCalendar (.ics). Sem <c>slug</c>: todos os próximos eventos, para assinar no celular
/// ("Sincronizar com meu calendário"). Com <c>slug</c>: um evento só ("Pôr na agenda").
/// </summary>
public static class GetCalendar
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(2);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/events.ics", Handle).WithName(nameof(GetCalendar)).WithSummary("Agenda em .ics")
            .Produces(StatusCodes.Status200OK, contentType: "text/calendar");

    private static async Task<IResult> Handle(
        AppDbContext db, TimeProvider clock, IConfiguration configuration, CancellationToken ct, string? slug = null)
    {
        var now = clock.GetUtcNow();
        var query = db.Events.Where(e => e.IsPublished && e.Status != EventStatus.Cancelled);
        query = slug is null ? query.Where(e => e.StartsAt >= now.AddDays(-1)) : query.Where(e => e.Slug == slug);

        var events = await query.OrderBy(e => e.StartsAt).Take(100)
            .Select(e => new
            {
                e.Id, e.Slug, e.Title, e.StartsAt, e.UpdatedAt, CityName = e.City.Name,
                Place = e.Venue != null ? e.Venue.Name + (e.Venue.Address != null ? ", " + e.Venue.Address : "") : e.VenueNote,
            })
            .ToListAsync(ct);
        if (slug is not null && events.Count == 0) return TypedResults.NotFound();

        var site = configuration["Site:PublicUrl"];
        var ics = new StringBuilder()
            .AppendLine("BEGIN:VCALENDAR")
            .AppendLine("VERSION:2.0")
            .AppendLine("PRODID:-//Galo Irlanda//Agenda//PT")
            .AppendLine("CALSCALE:GREGORIAN")
            .AppendLine("X-WR-CALNAME:Galo Irlanda")
            .AppendLine("X-WR-TIMEZONE:Europe/Dublin");
        foreach (var e in events)
        {
            ics.AppendLine("BEGIN:VEVENT")
                .AppendLine($"UID:{e.Id}@galoirlanda")
                .AppendLine($"DTSTAMP:{Utc(e.UpdatedAt)}")
                .AppendLine($"DTSTART:{Utc(e.StartsAt)}")
                .AppendLine($"DTEND:{Utc(e.StartsAt + DefaultDuration)}")
                .AppendLine($"SUMMARY:{Escape(e.Title)}")
                .AppendLine($"LOCATION:{Escape(string.Join(", ", new[] { e.Place, e.CityName }.Where(p => !string.IsNullOrEmpty(p))))}")
                .AppendLine($"URL:{site}/#partidas")
                .AppendLine("END:VEVENT");
        }
        ics.AppendLine("END:VCALENDAR");

        var fileName = slug is null ? "galo-irlanda.ics" : $"{slug}.ics";
        return TypedResults.File(Encoding.UTF8.GetBytes(ics.ToString().Replace("\n", "\r\n")), "text/calendar; charset=utf-8", fileName);
    }

    private static string Utc(DateTimeOffset value) => value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'");

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
}
