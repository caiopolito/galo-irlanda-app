using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Venues;

/// <summary>Local de evento: pub parceiro, parque, quadra.</summary>
public static class CreateVenue
{
    public sealed record Request(
        string CityCode, string Name, string? Neighborhood, string? Address, string? MapsUrl, string? WebsiteUrl,
        bool IsPartner, bool? ShowsWithSound, string? Notes);

    public sealed record Response(Guid Id, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.CityCode).NotEmpty().Length(3);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
            RuleFor(x => x.MapsUrl).Must(VenueRules.BeHttps).WithMessage("Use um link https://.");
            RuleFor(x => x.WebsiteUrl).Must(VenueRules.BeHttps).WithMessage("Use um link https://.");
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle).WithName(nameof(CreateVenue)).WithSummary("Cria um local").WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Created<Response>, ValidationProblem>> Handle(
        Request request, AppDbContext db, CancellationToken ct)
    {
        var cityCode = request.CityCode.ToUpperInvariant();
        if (!await db.Cities.AnyAsync(c => c.Code == cityCode, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["cityCode"] = ["Cidade não encontrada."] });

        var venue = new Venue
        {
            CityCode = cityCode,
            Name = request.Name.Trim(),
            Neighborhood = request.Neighborhood,
            Address = request.Address,
            MapsUrl = request.MapsUrl,
            WebsiteUrl = request.WebsiteUrl,
            IsPartner = request.IsPartner,
            ShowsWithSound = request.ShowsWithSound,
            Notes = request.Notes,
        };
        db.Venues.Add(venue);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/admin/venues/{venue.Id}", new Response(venue.Id, venue.Version));
    }
}

internal static class VenueRules
{
    public static bool BeHttps(string? url) => url is null || url.StartsWith("https://", StringComparison.Ordinal);
}
