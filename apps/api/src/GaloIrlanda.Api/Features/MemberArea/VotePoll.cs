using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

public static class VotePoll
{
    public sealed record Request(Guid OptionId);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.OptionId).NotEmpty();
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/polls/{id:guid}/vote", Handle).WithName(nameof(VotePoll)).WithSummary("Vota numa votação")
            .WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<NoContent, NotFound, ValidationProblem, ProblemHttpResult>> Handle(
        Guid id, Request request, CurrentUser currentUser, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var userId = currentUser.Id!.Value;
        var now = clock.GetUtcNow();
        var poll = await db.Polls.Include(p => p.Options).FirstOrDefaultAsync(p => p.Id == id && p.IsPublished, ct);
        if (poll is null) return TypedResults.NotFound();
        if (!poll.IsOpen(now))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["optionId"] = ["Esta votação está fechada."] });
        if (poll.Options.All(o => o.Id != request.OptionId))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["optionId"] = ["Opção inválida."] });
        if (!(await MemberStatus.StateAsync(db, userId, ct)).IsActive)
            return TypedResults.Problem(statusCode: 403, title: "Só sócios com a mensalidade em dia votam.");
        if (await db.PollParticipations.AnyAsync(p => p.PollId == id && p.UserId == userId, ct))
            return TypedResults.Problem(statusCode: 409, title: "Você já votou nesta votação.");

        // Participação e voto na mesma transação. Em votação secreta o voto não leva o usuário.
        db.PollParticipations.Add(new PollParticipation { PollId = id, UserId = userId, VotedAt = now });
        db.PollBallots.Add(new PollBallot { PollId = id, OptionId = request.OptionId, UserId = poll.IsSecret ? null : userId });
        await db.SaveChangesAsync(ct); // corrida com voto duplo: a chave da participação vira 409

        return TypedResults.NoContent();
    }
}
