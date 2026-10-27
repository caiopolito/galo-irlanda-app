using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Venues;

public static class ListVenues
{
    public sealed record Item(
        Guid Id, string CityCode, string Name, string? Neighborhood, string? Address, string? MapsUrl, string? WebsiteUrl,
        bool IsPartner, bool? ShowsWithSound, string? Notes, bool IsActive, uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListVenues)).WithSummary("Lista os locais (filtro opcional por cidade)");

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, CancellationToken ct, string? cityCode = null)
    {
        var query = db.Venues.AsQueryable();
        if (cityCode is not null) query = query.Where(v => v.CityCode == cityCode.ToUpperInvariant());

        return TypedResults.Ok(await query
            .OrderBy(v => v.CityCode).ThenBy(v => v.Name)
            .Select(v => new Item(v.Id, v.CityCode, v.Name, v.Neighborhood, v.Address, v.MapsUrl, v.WebsiteUrl,
                v.IsPartner, v.ShowsWithSound, v.Notes, v.IsActive, v.Version))
            .ToListAsync(ct));
    }
}
