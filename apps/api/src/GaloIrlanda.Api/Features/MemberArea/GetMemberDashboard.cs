using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>Cabeçalho da área do sócio: perfil, número, situação da mensalidade, "madrugadas" e indicações.</summary>
public static class GetMemberDashboard
{
    public sealed record Response(
        string FullName,
        string DisplayName,
        string Email,
        int MemberNumber,
        DateTimeOffset JoinedAt,
        string CityCode,
        string? Phone,
        bool PhotoTagOptIn,
        MembershipState Membership,
        int Attended,
        string ReferralCode,
        int Referrals,
        uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/me", Handle).WithName(nameof(GetMemberDashboard)).WithSummary("Painel do sócio");

    private static async Task<Results<Ok<Response>, NotFound>> Handle(
        CurrentUser currentUser, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var userId = currentUser.Id!.Value;
        var now = clock.GetUtcNow();
        var member = await db.Members.Include(m => m.User).FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (member is null) return TypedResults.NotFound();

        var attended = await db.EventRsvps.CountAsync(r => r.UserId == userId && r.Answer == RsvpAnswer.Going &&
            db.Events.Any(e => e.Id == r.EventId && e.StartsAt < now), ct);
        var referrals = await db.Members.CountAsync(m => m.ReferredBy == userId, ct);

        return TypedResults.Ok(new Response(member.FullName, member.User.DisplayName, member.User.Email,
            member.MemberNumber, member.JoinedAt, member.CityCode.Trim(), member.Phone, member.PhotoTagOptIn,
            await MemberStatus.StateAsync(db, userId, ct), attended, member.ReferralCode, referrals, member.Version));
    }
}
