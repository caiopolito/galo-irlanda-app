using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Eventos de webhook já recebidos: o Stripe reenvia, e cada evento só pode ser processado uma vez.</summary>
public sealed class StripeEvent
{
    /// <summary>Id do Stripe (evt_...).</summary>
    public required string Id { get; init; }
    public required string Type { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? Error { get; set; }
}

internal sealed class StripeEventConfiguration : IEntityTypeConfiguration<StripeEvent>
{
    public void Configure(EntityTypeBuilder<StripeEvent> b)
    {
        b.Property(e => e.Id).HasMaxLength(100);
        b.Property(e => e.Type).HasMaxLength(100);
        b.Property(e => e.Error).HasMaxLength(1000);
    }
}
