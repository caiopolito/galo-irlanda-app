using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Venues;

/// <summary>Edita um local. Para "excluir", desative (<c>isActive=false</c>): eventos antigos continuam apontando para ele.</summary>
public static class UpdateVenue
{
    public sealed record Request(
        string Name, string? Neighborhood, string? Address, string? MapsUrl, string? WebsiteUrl,
        bool IsPartner, bool? ShowsWithSound, string? Notes, bool IsActive, uint Version);

    public sealed record Response(Guid Id, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
            RuleFor(x => x.MapsUrl).Must(VenueRules.BeHttps).WithMessage("Use um link https://.");
            RuleFor(x => x.WebsiteUrl).Must(VenueRules.BeHttps).WithMessage("Use um link https://.");
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:guid}", Handle).WithName(nameof(UpdateVenue)).WithSummary("Edita um local").WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (venue is null) return TypedResults.NotFound();

        db.Entry(venue).Property(v => v.Version).OriginalValue = request.Version;
        venue.Name = request.Name.Trim();
        venue.Neighborhood = request.Neighborhood;
        venue.Address = request.Address;
        venue.MapsUrl = request.MapsUrl;
        venue.WebsiteUrl = request.WebsiteUrl;
        venue.IsPartner = request.IsPartner;
        venue.ShowsWithSound = request.ShowsWithSound;
        venue.Notes = request.Notes;
        venue.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new Response(venue.Id, venue.Version));
    }
}
