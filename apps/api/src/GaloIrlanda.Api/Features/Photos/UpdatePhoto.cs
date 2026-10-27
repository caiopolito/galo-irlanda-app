using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Photos;

/// <summary>Legenda, consentimento e onde a foto aparece. Foto pública exige texto alternativo e consentimento (GDPR).</summary>
public static class UpdatePhoto
{
    public sealed record Request(
        string? AltText,
        string? Title,
        string? Caption,
        DateOnly? TakenOn,
        Guid? EventId,
        PhotoVisibility Visibility,
        ConsentBasis ConsentBasis,
        string? ConsentNote,
        bool WallFeatured,
        int? WallSortOrder,
        uint Version);

    public sealed record Response(Guid Id, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Visibility).IsInEnum();
            RuleFor(x => x.ConsentBasis).IsInEnum();
            RuleFor(x => x.AltText).MaximumLength(300);
            RuleFor(x => x.Title).MaximumLength(120);
            RuleFor(x => x.Caption).MaximumLength(300);
            RuleFor(x => x.ConsentNote).MaximumLength(500);
            RuleFor(x => x.AltText).NotEmpty().When(x => x.Visibility == PhotoVisibility.Public)
                .WithMessage("Foto pública precisa de descrição (texto alternativo).");
            RuleFor(x => x.ConsentBasis)
                .Must(c => c is ConsentBasis.ConsentRecorded or ConsentBasis.NoIdentifiablePeople)
                .When(x => x.Visibility == PhotoVisibility.Public)
                .WithMessage("Foto pública precisa de consentimento registrado ou não mostrar pessoas identificáveis.");
            RuleFor(x => x.WallFeatured).Equal(false).When(x => x.Visibility != PhotoVisibility.Public)
                .WithMessage("Só fotos públicas vão para o mural.");
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:guid}", Handle)
            .WithName(nameof(UpdatePhoto))
            .WithSummary("Edita legenda, consentimento e publicação de uma foto")
            .WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<Response>, NotFound, ValidationProblem>> Handle(
        Guid id, Request request, AppDbContext db, CurrentUser currentUser, TimeProvider clock, CancellationToken ct)
    {
        var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, ct);
        if (photo is null) return TypedResults.NotFound();

        if (request.EventId is { } eventId && !await db.Events.AnyAsync(e => e.Id == eventId, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["eventId"] = ["Evento não encontrado."] });

        db.Entry(photo).Property(p => p.Version).OriginalValue = request.Version;

        // Quem confirmou o consentimento e quando: registrado na primeira vez que ele passa a valer.
        var consentGiven = request.ConsentBasis is ConsentBasis.ConsentRecorded or ConsentBasis.NoIdentifiablePeople;
        if (consentGiven && photo.ConsentBasis != request.ConsentBasis)
        {
            photo.ConsentConfirmedBy = currentUser.Id;
            photo.ConsentConfirmedAt = clock.GetUtcNow();
        }

        photo.AltText = request.AltText;
        photo.Title = request.Title;
        photo.Caption = request.Caption;
        photo.TakenOn = request.TakenOn;
        photo.EventId = request.EventId;
        photo.Visibility = request.Visibility;
        photo.ConsentBasis = request.ConsentBasis;
        photo.ConsentNote = request.ConsentNote;
        photo.WallFeatured = request.WallFeatured;
        photo.WallSortOrder = request.WallSortOrder;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new Response(photo.Id, photo.Version));
    }
}
