using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Milestones;

public static class DeleteMilestone
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:guid}", Handle).WithName(nameof(DeleteMilestone)).WithSummary("Exclui um marco")
            .RequireAuthorization(Policies.Admin);

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, AppDbContext db, CancellationToken ct)
    {
        var milestone = await db.Milestones.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (milestone is null) return TypedResults.NotFound();

        db.Milestones.Remove(milestone);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
