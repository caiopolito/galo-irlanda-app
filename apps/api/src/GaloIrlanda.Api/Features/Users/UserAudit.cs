using System.Text.Json;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;

namespace GaloIrlanda.Api.Features.Users;

/// <summary>
/// Usuário não passa pelo histórico automático (tem e-mail). Aqui registramos só a ação e os nomes dos
/// campos, nunca os valores (docs/database/schema.md).
/// </summary>
internal static class UserAudit
{
    public static void Add(AppDbContext db, Guid? actorId, Guid userId, string action, DateTimeOffset now, object? changes = null) =>
        db.AuditEntries.Add(new AuditEntry
        {
            OccurredAt = now,
            ActorUserId = actorId,
            Action = action,
            EntityType = "user",
            EntityId = userId.ToString(),
            Changes = changes is null ? null : JsonSerializer.Serialize(changes),
        });
}
