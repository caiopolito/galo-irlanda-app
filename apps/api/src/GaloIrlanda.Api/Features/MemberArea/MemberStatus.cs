using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>Situação da mensalidade de um sócio, a partir da assinatura mais recente.</summary>
public sealed record MembershipState(string Status, bool IsActive, DateTimeOffset? CurrentPeriodEnd, bool CancelAtPeriodEnd)
{
    public static readonly MembershipState None = new("none", false, null, false);
}

internal static class MemberStatus
{
    public static async Task<Subscription?> LatestSubscriptionAsync(AppDbContext db, Guid userId, CancellationToken ct) =>
        await db.Subscriptions.Where(s => s.UserId == userId).OrderByDescending(s => s.CreatedAt).FirstOrDefaultAsync(ct);

    public static async Task<MembershipState> StateAsync(AppDbContext db, Guid userId, CancellationToken ct) =>
        await LatestSubscriptionAsync(db, userId, ct) is { } s
            ? new MembershipState(s.Status, s.IsActive, s.CurrentPeriodEnd, s.CancelAtPeriodEnd)
            : MembershipState.None;
}
