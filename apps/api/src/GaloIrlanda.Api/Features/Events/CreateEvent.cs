using FluentValidation;
using GaloIrlanda.Api.Common.Text;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Events;

public static class CreateEvent
{
    public sealed record Request(
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
        int? Capacity,
        int SpotsTaken,
        bool Publish);

    public sealed record Response(Guid Id, string Slug, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Kind).IsInEnum();
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
            RuleFor(x => x.StartsAt).NotEmpty();
            RuleFor(x => x.CityCode).NotEmpty().Length(3);
            RuleFor(x => x.Opponent).NotEmpty().MaximumLength(80).When(x => x.Kind == EventKind.Match)
                .WithMessage("Jogo precisa de adversário.");
            RuleFor(x => x.VenueNote).NotEmpty().When(x => x.VenueId is null)
                .WithMessage("Escolha um local cadastrado ou descreva o local.");
            RuleFor(x => x.VenueNote).MaximumLength(120);
            RuleFor(x => x.HomeTrip).MaximumLength(200);
            RuleFor(x => x.Capacity).GreaterThan(0).When(x => x.Capacity is not null);
            RuleFor(x => x.SpotsTaken).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SpotsTaken).LessThanOrEqualTo(x => x.Capacity ?? int.MaxValue)
                .WithMessage("Vagas ocupadas não podem passar da capacidade.");
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName(nameof(CreateEvent))
            .WithSummary("Cria um jogo ou encontro")
            .WithValidation<Request>();

    private static async Task<Results<Created<Response>, ValidationProblem>> Handle(
        Request request, AppDbContext db, CancellationToken ct)
    {
        var cityCode = request.CityCode.ToUpperInvariant();
        if (!await db.Cities.AnyAsync(c => c.Code == cityCode, ct))
            return Invalid("cityCode", "Cidade não encontrada.");

        if (request.VenueId is { } venueId &&
            !await db.Venues.AnyAsync(v => v.Id == venueId && v.CityCode == cityCode && v.IsActive, ct))
            return Invalid("venueId", "Local não encontrado nesta cidade.");

        if (request.CoverPhotoId is { } photoId &&
            !await db.Photos.AnyAsync(p => p.Id == photoId && p.DeletedAt == null, ct))
            return Invalid("coverPhotoId", "Foto não encontrada.");

        var startsAt = request.StartsAt.ToUniversalTime();
        var ev = new Event
        {
            Slug = Slug.From($"{startsAt:yyyy-MM-dd} {request.Title}"),
            Kind = request.Kind,
            Title = request.Title.Trim(),
            StartsAt = startsAt,
            CityCode = cityCode,
            VenueId = request.VenueId,
            VenueNote = request.VenueNote,
            Opponent = request.Opponent,
            Competition = request.Competition,
            HomeTrip = request.HomeTrip,
            DescriptionMd = request.DescriptionMd,
            CoverPhotoId = request.CoverPhotoId,
            Status = request.Status,
            Capacity = request.Capacity,
            SpotsTaken = request.SpotsTaken,
            IsPublished = request.Publish,
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/admin/events/{ev.Id}", new Response(ev.Id, ev.Slug, ev.Version));
    }

    private static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
