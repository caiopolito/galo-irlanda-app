using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Histórico de alterações: quem mudou o quê e quando. Nunca guarda e-mail, token, votos ou RSVP.</summary>
public sealed class AuditEntry
{
    public long Id { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public Guid? ActorUserId { get; init; }
    public required string Action { get; init; }
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }
    /// <summary>JSON <c>{"campo": [antigo, novo]}</c> só com os campos alterados.</summary>
    public string? Changes { get; set; }
}

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("audit_log");
        b.Property(a => a.Id).UseIdentityAlwaysColumn();
        b.Property(a => a.Changes).HasColumnType("jsonb");
        b.HasOne<User>().WithMany().HasForeignKey(a => a.ActorUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(a => new { a.EntityType, a.EntityId, a.OccurredAt }).IsDescending(false, false, true);
    }
}
