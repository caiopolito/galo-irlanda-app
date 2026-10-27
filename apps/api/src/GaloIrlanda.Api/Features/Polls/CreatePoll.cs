using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Polls;

public static class CreatePoll
{
    public sealed record Request(
        string Title, string? Description, string? HandNote, bool IsSecret, DateTimeOffset OpensAt, DateTimeOffset ClosesAt,
        IReadOnlyList<string> Options, bool Publish);

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
        group.MapPost("/", Handle).WithName(nameof(CreatePoll)).WithSummary("Cria uma votação").WithValidation<Request>();

    private static async Task<Created<Response>> Handle(Request request, AppDbContext db, CancellationToken ct)
    {
        var poll = new Poll
        {
            Title = request.Title.Trim(),
            Description = request.Description,
            HandNote = request.HandNote,
            IsSecret = request.IsSecret,
            OpensAt = request.OpensAt.ToUniversalTime(),
            ClosesAt = request.ClosesAt.ToUniversalTime(),
            IsPublished = request.Publish,
        };
        for (short i = 0; i < request.Options.Count; i++)
            poll.Options.Add(new PollOption { PollId = poll.Id, Label = request.Options[i].Trim(), SortOrder = i });

        db.Polls.Add(poll);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/admin/polls/{poll.Id}", new Response(poll.Id, poll.Version));
    }
}
