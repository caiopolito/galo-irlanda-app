using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Milestones;

/// <summary>Novo marco. Publique só depois de conferir o fato numa fonte oficial (DESIGN.md: "Não inventar dados").</summary>
public static class CreateMilestone
{
    public sealed record Request(short Year, string Text, Guid? PhotoId, short SortOrder, bool IsPublished);

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
        group.MapPost("/", Handle).WithName(nameof(CreateMilestone)).WithSummary("Cria um marco").WithValidation<Request>();

    private static async Task<Created<Response>> Handle(Request request, AppDbContext db, CancellationToken ct)
    {
        var milestone = new Milestone
        {
            Year = request.Year,
            Text = request.Text.Trim(),
            PhotoId = request.PhotoId,
            SortOrder = request.SortOrder,
            IsPublished = request.IsPublished,
        };
        db.Milestones.Add(milestone);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/admin/milestones/{milestone.Id}", new Response(milestone.Id, milestone.Version));
    }
}
