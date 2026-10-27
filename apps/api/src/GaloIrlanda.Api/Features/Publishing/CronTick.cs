using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Publishing;

/// <summary>
/// Chamado a cada 15 min por um Cron Trigger da Cloudflare: publica posts agendados que venceram,
/// limpa dados expirados e dispara a regeração pendente.
/// </summary>
public static class CronTick
{
    public sealed record Response(int DuePosts, int ExpiredTokens, int OldSessions, bool Triggered);

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/cron/tick", Handle).WithName(nameof(CronTick));

    private static async Task<Ok<Response>> Handle(
        AppDbContext db, SiteRebuilder rebuilder, TimeProvider clock, CancellationToken ct)
    {
        var now = clock.GetUtcNow();

        // Posts agendados que passaram do horário desde o último disparo.
        var lastTriggeredAt = await db.SiteRebuildRequests.MaxAsync(r => r.TriggeredAt, ct) ?? DateTimeOffset.MinValue;
        var duePosts = await db.Posts.CountAsync(p =>
            p.Status == PostStatus.Published && p.PublishedAt > lastTriggeredAt && p.PublishedAt <= now, ct);
        if (duePosts > 0)
        {
            db.SiteRebuildRequests.Add(new SiteRebuildRequest
            {
                RequestedAt = now,
                Reason = "scheduled_posts",
            });
            await db.SaveChangesAsync(ct);
        }

        var expiredTokens = await db.LoginTokens
            .Where(t => t.ExpiresAt < now.AddHours(-24))
            .ExecuteDeleteAsync(ct);

        // Retenção (docs/database/schema.md): sessões somem 7 dias após expirar ou serem revogadas.
        var sessionCutoff = now.AddDays(-7);
        var oldSessions = await db.Sessions
            .Where(s => s.ExpiresAt < sessionCutoff || s.RevokedAt < sessionCutoff)
            .ExecuteDeleteAsync(ct);

        var triggered = await rebuilder.TryTriggerAsync(force: false, ct);
        return TypedResults.Ok(new Response(duePosts, expiredTokens, oldSessions, triggered));
    }
}
