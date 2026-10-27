using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Members;

/// <summary>Sócios no admin, com a situação da mensalidade. Busca por nome, e-mail ou número.</summary>
public static class ListMembers
{
    public sealed record Item(
        Guid UserId, int MemberNumber, string FullName, string Email, string CityCode, DateTimeOffset JoinedAt,
        string Status, DateTimeOffset? PaidUntil, bool PhotoTagOptIn);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListMembers)).WithSummary("Lista os sócios");

    private static async Task<Ok<PagedResponse<Item>>> Handle(
        [AsParameters] PageRequest page, AppDbContext db, CancellationToken ct, string? search = null, bool? photoTagOptIn = null)
    {
        var query = db.Members.Where(m => m.User.Status != UserStatus.Erased);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = int.TryParse(term, out var number)
                ? query.Where(m => m.MemberNumber == number)
                : query.Where(m => EF.Functions.ILike(m.FullName, $"%{term}%") || EF.Functions.ILike(m.User.Email, $"%{term}%"));
        }
        if (photoTagOptIn is not null) query = query.Where(m => m.PhotoTagOptIn == photoTagOptIn);

        var result = await query
            .OrderBy(m => m.MemberNumber)
            .Select(m => new
            {
                m.UserId, m.MemberNumber, m.FullName, m.User.Email, m.CityCode, m.JoinedAt, m.PhotoTagOptIn,
                Subscription = db.Subscriptions.Where(s => s.UserId == m.UserId).OrderByDescending(s => s.CreatedAt)
                    .Select(s => new { s.Status, s.CurrentPeriodEnd }).FirstOrDefault(),
            })
            .ToPagedAsync(page, ct);

        var items = result.Items.Select(m => new Item(m.UserId, m.MemberNumber, m.FullName, m.Email, m.CityCode.Trim(),
            m.JoinedAt, m.Subscription?.Status ?? "none", m.Subscription?.CurrentPeriodEnd, m.PhotoTagOptIn)).ToList();
        return TypedResults.Ok(new PagedResponse<Item>(items, result.Page, result.PageSize, result.Total));
    }
}
