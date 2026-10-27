using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>"Vou / Talvez / Não vou". Só para eventos publicados que ainda não terminaram.</summary>
public static class SetRsvp
{
    public sealed record Request(RsvpAnswer Answer);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Answer).IsInEnum();
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/events/{id:guid}/rsvp", Handle).WithName(nameof(SetRsvp)).WithSummary("Responde se vai ao evento")
            .WithValidation<Request>();

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> Handle(
        Guid id, Request request, CurrentUser currentUser, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id && e.IsPublished, ct);
        if (ev is null) return TypedResults.NotFound();
        if (ev.Status == EventStatus.Cancelled || ev.StartsAt < now.AddHours(-3))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["answer"] = ["Este evento não recebe mais respostas."] });

        var userId = currentUser.Id!.Value;
        if (ev.Capacity is { } capacity && request.Answer == RsvpAnswer.Going)
        {
            var going = ev.SpotsTaken + await db.EventRsvps.CountAsync(
                r => r.EventId == id && r.Answer == RsvpAnswer.Going && r.UserId != userId, ct);
            if (going >= capacity)
                return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["answer"] = ["As vagas acabaram."] });
        }

        var rsvp = await db.EventRsvps.FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId, ct);
        if (rsvp is null)
        {
            rsvp = new EventRsvp { EventId = id, UserId = userId };
            db.EventRsvps.Add(rsvp);
        }
        rsvp.Answer = request.Answer;
        rsvp.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
