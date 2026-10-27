using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Pedido de regeração do site público, gravado na mesma transação da mudança (outbox).</summary>
public sealed class SiteRebuildRequest
{
    public long Id { get; init; }
    public DateTimeOffset RequestedAt { get; init; }
    public Guid? RequestedBy { get; init; }
    public required string Reason { get; init; }
    public DateTimeOffset? TriggeredAt { get; set; }
}

internal sealed class SiteRebuildRequestConfiguration : IEntityTypeConfiguration<SiteRebuildRequest>
{
    public void Configure(EntityTypeBuilder<SiteRebuildRequest> b)
    {
        b.Property(r => r.Id).UseIdentityAlwaysColumn();
        b.Property(r => r.Reason).HasMaxLength(120);
        b.HasOne<User>().WithMany().HasForeignKey(r => r.RequestedBy).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(r => r.RequestedAt).HasFilter("triggered_at IS NULL");
    }
}
