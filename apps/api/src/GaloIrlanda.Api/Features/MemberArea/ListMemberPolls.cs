using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>
/// Votações para o sócio. O resultado aparece depois que ele vota ou quando a votação fecha.
/// Em votação secreta, nem a API sabe em que opção o sócio votou.
/// </summary>
public static class ListMemberPolls
{
    public sealed record Option(Guid Id, string Label, int? Votes);

    public sealed record Item(
        Guid Id,
        string Title,
        string? Description,
        string? HandNote,
        bool IsSecret,
        DateTimeOffset ClosesAt,
        bool IsOpen,
        bool HasVoted,
        Guid? MyOptionId,
        int? TotalVotes,
        IReadOnlyList<Option> Options);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/polls", Handle).WithName(nameof(ListMemberPolls)).WithSummary("Votações da torcida");

    private static async Task<Ok<List<Item>>> Handle(
        CurrentUser currentUser, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var userId = currentUser.Id!.Value;
        var now = clock.GetUtcNow();
        var recentlyClosed = now.AddDays(-30);

        var polls = await db.Polls
            .Include(p => p.Options)
            .Where(p => p.IsPublished && p.OpensAt <= now && p.ClosesAt >= recentlyClosed)
            .OrderBy(p => p.ClosesAt < now).ThenBy(p => p.ClosesAt)
            .ToListAsync(ct);
        var pollIds = polls.Select(p => p.Id).ToList();

        var voted = await db.PollParticipations.Where(p => p.UserId == userId && pollIds.Contains(p.PollId))
            .Select(p => p.PollId).ToListAsync(ct);
        var myOpenBallots = await db.PollBallots.Where(b => b.UserId == userId && pollIds.Contains(b.PollId))
            .ToDictionaryAsync(b => b.PollId, b => b.OptionId, ct);
        var counts = await db.PollBallots.Where(b => pollIds.Contains(b.PollId))
            .GroupBy(b => b.OptionId).Select(g => new { OptionId = g.Key, Votes = g.Count() })
            .ToDictionaryAsync(x => x.OptionId, x => x.Votes, ct);

        return TypedResults.Ok(polls.Select(p =>
        {
            var hasVoted = voted.Contains(p.Id);
            var isOpen = p.IsOpen(now);
            var showResults = hasVoted || !isOpen;
            return new Item(p.Id, p.Title, p.Description, p.HandNote, p.IsSecret, p.ClosesAt, isOpen, hasVoted,
                p.IsSecret ? null : myOpenBallots.GetValueOrDefault(p.Id) is var mine && mine != Guid.Empty ? mine : null,
                showResults ? p.Options.Sum(o => counts.GetValueOrDefault(o.Id)) : null,
                p.Options.OrderBy(o => o.SortOrder)
                    .Select(o => new Option(o.Id, o.Label, showResults ? counts.GetValueOrDefault(o.Id) : null))
                    .ToList());
        }).ToList());
    }
}
