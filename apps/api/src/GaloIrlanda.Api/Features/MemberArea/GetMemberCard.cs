using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>Carteirinha do sócio: dados do cartão de embarque, QR code (SVG) e PDF.</summary>
public static class GetMemberCard
{
    public sealed record Response(
        string FullName, int MemberNumber, DateTimeOffset? ValidUntil, bool IsActive, string VerificationUrl, string QrSvg);

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/card", Handle).WithName(nameof(GetMemberCard)).WithSummary("Carteirinha com QR code");
        group.MapGet("/card.pdf", Pdf).WithName("DownloadMemberCard").WithSummary("Carteirinha em PDF")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
    }

    private static async Task<Results<Ok<Response>, NotFound>> Handle(
        CurrentUser currentUser, AppDbContext db, MemberCard card, CancellationToken ct)
    {
        var userId = currentUser.Id!.Value;
        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (member is null) return TypedResults.NotFound();

        var state = await MemberStatus.StateAsync(db, userId, ct);
        var url = card.VerificationUrl(card.Token(member));
        return TypedResults.Ok(new Response(member.FullName, member.MemberNumber, state.CurrentPeriodEnd, state.IsActive,
            url, MemberCard.QrSvg(url)));
    }

    private static async Task<Results<FileContentHttpResult, NotFound>> Pdf(
        CurrentUser currentUser, AppDbContext db, MemberCard card, CancellationToken ct)
    {
        var userId = currentUser.Id!.Value;
        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (member is null) return TypedResults.NotFound();

        var state = await MemberStatus.StateAsync(db, userId, ct);
        var pdf = MemberCard.Pdf(member, card.VerificationUrl(card.Token(member)), state.CurrentPeriodEnd);
        return TypedResults.File(pdf, "application/pdf", $"carteirinha-galo-irlanda-{member.MemberNumber:0000}.pdf");
    }
}
