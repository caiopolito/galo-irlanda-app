using System.Text.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GaloIrlanda.Api.Data;

/// <summary>
/// Antes de salvar: preenche datas e autor, grava o histórico de alterações e, se algum conteúdo
/// público mudou, registra um pedido de regeração do site na mesma transação (ADR 0015).
/// </summary>
public sealed class AuditingInterceptor(CurrentUser currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    // Campos que não entram no diff: controle interno ou texto longo.
    private static readonly HashSet<string> IgnoredProperties =
        [nameof(AuditableEntity.CreatedAt), nameof(AuditableEntity.CreatedBy),
         nameof(AuditableEntity.UpdatedAt), nameof(AuditableEntity.UpdatedBy), "Version"];

    private static readonly HashSet<string> LongTextProperties = [nameof(Post.BodyMd), nameof(Event.DescriptionMd)];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is AppDbContext db) Stamp(db);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext db) Stamp(db);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(AppDbContext db)
    {
        db.ChangeTracker.DetectChanges();
        var now = clock.GetUtcNow();
        var userId = currentUser.Id;
        var rebuildReasons = new List<string>();

        foreach (var entry in db.ChangeTracker.Entries<ITimestamped>().ToList())
        {
            if (entry.State is EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State is EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in db.ChangeTracker.Entries<AuditableEntity>().ToList())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            if (entry.State is EntityState.Added) entry.Entity.CreatedBy = userId;
            if (entry.State is not EntityState.Deleted) entry.Entity.UpdatedBy = userId;

            var action = entry.State switch
            {
                EntityState.Added => "create",
                EntityState.Deleted => "delete",
                _ => "update",
            };
            var entityType = JsonNamingPolicy.SnakeCaseLower.ConvertName(entry.Metadata.ClrType.Name);

            db.AuditEntries.Add(new AuditEntry
            {
                OccurredAt = now,
                ActorUserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = KeyOf(entry),
                Changes = entry.State is EntityState.Modified ? Diff(entry) : null,
            });

            if (TouchesPublicSite(entry)) rebuildReasons.Add($"{entityType}:{action}");
        }

        if (rebuildReasons.Count > 0)
        {
            db.SiteRebuildRequests.Add(new SiteRebuildRequest
            {
                RequestedAt = now,
                RequestedBy = userId,
                Reason = string.Join(", ", rebuildReasons.Distinct()).Truncate(120),
            });
        }
    }

    private static bool TouchesPublicSite(EntityEntry entry)
    {
        if (entry.Entity is not IPublicContent current) return false;
        if (entry.State is EntityState.Added) return current.IsPublic;
        // Despublicar também muda o site: compara com o estado antes da alteração.
        var wasPublic = entry.OriginalValues.ToObject() is IPublicContent { IsPublic: true };
        return entry.State is EntityState.Deleted ? wasPublic : wasPublic || current.IsPublic;
    }

    private static string KeyOf(EntityEntry entry) =>
        string.Join("|", entry.Metadata.FindPrimaryKey()!.Properties.Select(p => entry.Property(p.Name).CurrentValue));

    private static string? Diff(EntityEntry entry)
    {
        var changes = new Dictionary<string, object?[]>();
        foreach (var property in entry.Properties)
        {
            var name = property.Metadata.Name;
            if (!property.IsModified || IgnoredProperties.Contains(name)) continue;
            if (Equals(property.OriginalValue, property.CurrentValue)) continue;

            var column = JsonNamingPolicy.SnakeCaseLower.ConvertName(name);
            changes[column] = LongTextProperties.Contains(name)
                ? ["(alterado)"]
                : [property.OriginalValue, property.CurrentValue];
        }
        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes);
    }
}

internal static class StringExtensions
{
    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
