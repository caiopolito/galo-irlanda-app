using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Settings;

/// <summary>Configurações gerais do site (linha única): foto da home, mensalidade e links.</summary>
public static class GetSiteSettings
{
    public sealed record Response(
        Guid? HeroPhotoId, int? MembershipFeeCents, string MembershipCurrency, string? WhatsappUrl,
        string? InstagramUrl, string? ContactEmail, DateTimeOffset UpdatedAt, uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(GetSiteSettings)).WithSummary("Configurações do site");

    private static async Task<Ok<Response>> Handle(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.SiteSettings
            .Select(s => new Response(s.HeroPhotoId, s.MembershipFeeCents, s.MembershipCurrency, s.WhatsappUrl,
                s.InstagramUrl, s.ContactEmail, s.UpdatedAt, s.Version))
            .SingleAsync(ct));
}
