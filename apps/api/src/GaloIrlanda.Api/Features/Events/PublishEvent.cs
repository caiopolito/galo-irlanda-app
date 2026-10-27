using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Events;

/// <summary>Coloca o evento no site público (ou tira, com <c>unpublish</c>).</summary>
public static class PublishEvent
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/publish", (Guid id, AppDbContext db, CancellationToken ct) => SetPublished(id, true, db, ct))
            .WithName(nameof(PublishEvent))
            .WithSummary("Publica o evento no site");
        group.MapPost("/{id:guid}/unpublish", (Guid id, AppDbContext db, CancellationToken ct) => SetPublished(id, false, db, ct))
            .WithName("UnpublishEvent")
            .WithSummary("Tira o evento do site");
    }

    private static async Task<Results<NoContent, NotFound>> SetPublished(
        Guid id, bool published, AppDbContext db, CancellationToken ct)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null) return TypedResults.NotFound();

        ev.IsPublished = published;
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
