using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.History;

/// <summary>Histórico de alterações: quem mudou o quê e quando. Filtra por tipo (event, post...) e item.</summary>
public static class ListChanges
{
    public sealed record Item(
        long Id, DateTimeOffset OccurredAt, string? ActorName, string Action, string EntityType, string EntityId, string? Changes);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/", Handle).WithName(nameof(ListChanges)).WithSummary("Histórico de alterações");

    private static async Task<Ok<PagedResponse<Item>>> Handle(
        [AsParameters] PageRequest page, AppDbContext db, CancellationToken ct, string? entityType = null, string? entityId = null)
    {
        var query = db.AuditEntries.AsQueryable();
        if (entityType is not null) query = query.Where(a => a.EntityType == entityType);
        if (entityId is not null) query = query.Where(a => a.EntityId == entityId);

        var result = await query
            .OrderByDescending(a => a.Id)
            .Select(a => new Item(
                a.Id, a.OccurredAt,
                db.Users.Where(u => u.Id == a.ActorUserId).Select(u => u.DisplayName).FirstOrDefault(),
                a.Action, a.EntityType, a.EntityId, a.Changes))
            .ToPagedAsync(page, ct);

        return TypedResults.Ok(result);
    }
}
