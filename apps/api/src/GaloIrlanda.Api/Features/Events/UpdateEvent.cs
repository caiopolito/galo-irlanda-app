using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Events;

/// <summary>Edita um evento. O slug não muda depois de criado (links já compartilhados).</summary>
public static class UpdateEvent
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
        EventStatus Status,
        int? Capacity,
        int SpotsTaken,
        uint Version);

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
        group.MapPut("/{id:guid}", Handle)
            .WithName(nameof(UpdateEvent))
            .WithSummary("Edita um jogo ou encontro")
            .WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<Response>, NotFound, ValidationProblem>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null) return TypedResults.NotFound();

        var cityCode = request.CityCode.ToUpperInvariant();
        if (!await db.Cities.AnyAsync(c => c.Code == cityCode, ct))
            return Invalid("cityCode", "Cidade não encontrada.");

        if (request.VenueId is { } venueId &&
            !await db.Venues.AnyAsync(v => v.Id == venueId && v.CityCode == cityCode && v.IsActive, ct))
            return Invalid("venueId", "Local não encontrado nesta cidade.");

        // Concorrência otimista: se outra pessoa salvou antes, o SaveChanges lança e vira 409.
        db.Entry(ev).Property(e => e.Version).OriginalValue = request.Version;

        ev.Kind = request.Kind;
        ev.Title = request.Title.Trim();
        ev.StartsAt = request.StartsAt.ToUniversalTime();
        ev.CityCode = cityCode;
        ev.VenueId = request.VenueId;
        ev.VenueNote = request.VenueNote;
        ev.Opponent = request.Opponent;
        ev.Competition = request.Competition;
        ev.HomeTrip = request.HomeTrip;
        ev.DescriptionMd = request.DescriptionMd;
        ev.Status = request.Status;
        ev.Capacity = request.Capacity;
        ev.SpotsTaken = request.SpotsTaken;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new Response(ev.Id, ev.Slug, ev.Version));
    }

    private static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
