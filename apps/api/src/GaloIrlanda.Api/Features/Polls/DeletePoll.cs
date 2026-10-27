using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Polls;

public static class DeletePoll
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:guid}", Handle).WithName(nameof(DeletePoll)).WithSummary("Exclui uma votação (e os votos)")
            .RequireAuthorization(Policies.Admin);

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, AppDbContext db, CancellationToken ct)
    {
        var poll = await db.Polls.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (poll is null) return TypedResults.NotFound();

        db.Polls.Remove(poll);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
