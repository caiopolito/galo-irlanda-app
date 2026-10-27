using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Polls;

/// <summary>Edita a votação. Depois do primeiro voto, opções e sigilo ficam travados (mudar alteraria votos já dados).</summary>
public static class UpdatePoll
{
    public sealed record Request(
        string Title, string? Description, string? HandNote, bool IsSecret, DateTimeOffset OpensAt, DateTimeOffset ClosesAt,
        IReadOnlyList<string> Options, bool IsPublished, uint Version);

    public sealed record Response(Guid Id, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            PollRules.Apply(this, x => x.Title, x => x.OpensAt, x => x.ClosesAt, x => x.Options);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.HandNote).MaximumLength(80);
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:guid}", Handle).WithName(nameof(UpdatePoll)).WithSummary("Edita uma votação")
            .WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<Response>, NotFound, ValidationProblem>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var poll = await db.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (poll is null) return TypedResults.NotFound();

        var options = request.Options.Select(o => o.Trim()).ToList();
        var optionsChanged = !poll.Options.OrderBy(o => o.SortOrder).Select(o => o.Label).SequenceEqual(options);
        if ((optionsChanged || poll.IsSecret != request.IsSecret) &&
            await db.PollParticipations.AnyAsync(p => p.PollId == id, ct))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["options"] = ["Já tem voto: opções e sigilo não podem mais mudar."],
            });
        }

        db.Entry(poll).Property(p => p.Version).OriginalValue = request.Version;
        poll.Title = request.Title.Trim();
        poll.Description = request.Description;
        poll.HandNote = request.HandNote;
        poll.IsSecret = request.IsSecret;
        poll.OpensAt = request.OpensAt.ToUniversalTime();
        poll.ClosesAt = request.ClosesAt.ToUniversalTime();
        poll.IsPublished = request.IsPublished;
        if (optionsChanged)
        {
            db.PollOptions.RemoveRange(poll.Options);
            poll.Options.Clear();
            for (short i = 0; i < options.Count; i++)
                poll.Options.Add(new PollOption { PollId = poll.Id, Label = options[i], SortOrder = i });
        }
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new Response(poll.Id, poll.Version));
    }
}
