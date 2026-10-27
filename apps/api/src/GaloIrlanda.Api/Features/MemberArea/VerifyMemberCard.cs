using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>
/// Verificação da carteirinha (o QR abre esta checagem). Usada pelo pub parceiro para dar o desconto:
/// mostra só o necessário — nome, número e se a mensalidade está em dia.
/// </summary>
public static class VerifyMemberCard
{
    public sealed record Response(bool Valid, string? FullName, int? MemberNumber, bool IsActive, DateTimeOffset? ValidUntil);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/card/{token}", Handle).WithName(nameof(VerifyMemberCard)).WithSummary("Confere uma carteirinha");

    private static async Task<Ok<Response>> Handle(string token, AppDbContext db, MemberCard card, CancellationToken ct)
    {
        var invalid = new Response(false, null, null, false, null);
        if (card.ReadToken(token) is not var (userId, version)) return TypedResults.Ok(invalid);

        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (member is null || member.CardKeyVersion != version) return TypedResults.Ok(invalid);

        var state = await MemberStatus.StateAsync(db, userId, ct);
        return TypedResults.Ok(new Response(true, member.FullName, member.MemberNumber, state.IsActive, state.CurrentPeriodEnd));
    }
}
