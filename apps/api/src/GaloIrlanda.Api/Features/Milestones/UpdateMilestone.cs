using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Milestones;

public static class UpdateMilestone
{
    public sealed record Request(short Year, string Text, Guid? PhotoId, short SortOrder, bool IsPublished, uint Version);

    public sealed record Response(Guid Id, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Year).InclusiveBetween((short)1900, (short)2100);
            RuleFor(x => x.Text).NotEmpty().MaximumLength(200);
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:guid}", Handle).WithName(nameof(UpdateMilestone)).WithSummary("Edita um marco")
            .WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<Response>, NotFound>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (milestone is null) return TypedResults.NotFound();

        db.Entry(milestone).Property(m => m.Version).OriginalValue = request.Version;
        milestone.Year = request.Year;
        milestone.Text = request.Text.Trim();
        milestone.PhotoId = request.PhotoId;
        milestone.SortOrder = request.SortOrder;
        milestone.IsPublished = request.IsPublished;
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new Response(milestone.Id, milestone.Version));
    }
}
