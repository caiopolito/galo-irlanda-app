using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Events;

/// <summary>Exclusão definitiva, só para administradores. O histórico fica no audit_log.</summary>
public static class DeleteEvent
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:guid}", Handle)
            .WithName(nameof(DeleteEvent))
            .WithSummary("Exclui um evento")
            .RequireAuthorization(Policies.Admin);

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, AppDbContext db, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null) return TypedResults.NotFound();

        db.Events.Remove(ev);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
