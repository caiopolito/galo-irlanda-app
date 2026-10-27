using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Polls;

/// <summary>Votações no admin, com participação e resultado parcial (a diretoria acompanha mesmo antes de fechar).</summary>
public static class ListPolls
{
    public sealed record Option(Guid Id, string Label, int Votes);

    public sealed record Item(
        Guid Id, string Title, string? Description, string? HandNote, bool IsSecret, DateTimeOffset OpensAt,
        DateTimeOffset ClosesAt, bool IsPublished, int Participants, IReadOnlyList<Option> Options, uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListPolls)).WithSummary("Lista as votações");

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, CancellationToken ct)
    {
        var polls = await db.Polls.Include(p => p.Options).OrderByDescending(p => p.ClosesAt).Take(50).ToListAsync(ct);
        var ids = polls.Select(p => p.Id).ToList();
        var participants = await db.PollParticipations.Where(p => ids.Contains(p.PollId))
            .GroupBy(p => p.PollId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var votes = await db.PollBallots.Where(b => ids.Contains(b.PollId))
            .GroupBy(b => b.OptionId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return TypedResults.Ok(polls.Select(p => new Item(p.Id, p.Title, p.Description, p.HandNote, p.IsSecret, p.OpensAt,
            p.ClosesAt, p.IsPublished, participants.GetValueOrDefault(p.Id),
            p.Options.OrderBy(o => o.SortOrder).Select(o => new Option(o.Id, o.Label, votes.GetValueOrDefault(o.Id))).ToList(),
            p.Version)).ToList());
    }
}
