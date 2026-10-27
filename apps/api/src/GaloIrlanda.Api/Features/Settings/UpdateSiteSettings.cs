using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Settings;

/// <summary>Configurações gerais do site (linha única): foto da home, mensalidade e links.</summary>
public static class UpdateSiteSettings
{
    public sealed record Request(
        Guid? HeroPhotoId, int? MembershipFeeCents, string? WhatsappUrl, string? InstagramUrl, string? ContactEmail,
        uint Version);

    public sealed record Response(uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.MembershipFeeCents).GreaterThanOrEqualTo(0);
            RuleFor(x => x.WhatsappUrl).Must(BeHttps).WithMessage("Use um link https://.");
            RuleFor(x => x.InstagramUrl).Must(BeHttps).WithMessage("Use um link https://.");
            RuleFor(x => x.ContactEmail).EmailAddress().When(x => x.ContactEmail is not null);
        }

        private static bool BeHttps(string? url) => url is null || url.StartsWith("https://", StringComparison.Ordinal);
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/", Handle).WithName(nameof(UpdateSiteSettings)).WithSummary("Edita as configurações do site")
            .WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<Response>, ValidationProblem>> Handle(
        Request request, AppDbContext db, CurrentUser currentUser, CancellationToken ct)
    {
        if (request.HeroPhotoId is { } photoId && !await db.Photos.AnyAsync(p => p.Id == photoId && p.DeletedAt == null, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["heroPhotoId"] = ["Foto não encontrada."] });

        var settings = await db.SiteSettings.SingleAsync(ct);
        db.Entry(settings).Property(s => s.Version).OriginalValue = request.Version;
        settings.HeroPhotoId = request.HeroPhotoId;
        settings.MembershipFeeCents = request.MembershipFeeCents;
        settings.WhatsappUrl = request.WhatsappUrl;
        settings.InstagramUrl = request.InstagramUrl;
        settings.ContactEmail = request.ContactEmail;
        settings.UpdatedBy = currentUser.Id;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new Response(settings.Version));
    }
}
