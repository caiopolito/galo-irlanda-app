using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Integrations.Cloudflare;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Publishing;

/// <summary>
/// Consome os pedidos pendentes de <c>site_rebuild_requests</c> e dispara o deploy hook.
/// Agrupa edições seguidas (espera mínima entre disparos); o que sobrar, o cron pega.
/// </summary>
public sealed class SiteRebuilder(AppDbContext db, IDeployHook hook, TimeProvider clock, ILogger<SiteRebuilder> log)
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromMinutes(2);

    public async Task<bool> TryTriggerAsync(bool force, CancellationToken ct)
    {
        var now = clock.GetUtcNow();

        if (!force)
        {
            var lastTriggeredAt = await db.SiteRebuildRequests.MaxAsync(r => r.TriggeredAt, ct);
            if (lastTriggeredAt > now - MinInterval) return false;
        }

        // Marca os pendentes de forma atômica: duas requisições ao mesmo tempo não disparam duas vezes.
        var claimed = await db.SiteRebuildRequests
            .Where(r => r.TriggeredAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.TriggeredAt, now), ct);
        if (claimed == 0 && !force) return false;

        try
        {
            await hook.TriggerAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            log.LogWarning(ex, "Deploy hook falhou; os pedidos voltam para a fila do próximo cron.");
            await db.SiteRebuildRequests
                .Where(r => r.TriggeredAt == now)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.TriggeredAt, (DateTimeOffset?)null), ct);
            return false;
        }
    }
}
