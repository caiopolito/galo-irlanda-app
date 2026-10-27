using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Assinatura da mensalidade no Stripe, que é a fonte da verdade (ADR 0008). Aqui fica só o espelho.</summary>
public sealed class Subscription : ITimestamped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid UserId { get; init; }
    public required string StripeCustomerId { get; init; }
    public required string StripeSubscriptionId { get; init; }
    /// <summary>Status do Stripe: active, trialing, past_due, unpaid, canceled, incomplete, incomplete_expired, paused.</summary>
    public required string Status { get; set; }
    public DateTimeOffset? CurrentPeriodEnd { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    /// <summary>Data do evento do Stripe que gerou o estado atual: eventos fora de ordem não sobrescrevem um mais novo.</summary>
    public DateTimeOffset LastEventAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static readonly string[] ActiveStatuses = ["active", "trialing"];

    public bool IsActive => ActiveStatuses.Contains(Status);
}

internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.HasIndex(s => s.StripeSubscriptionId).IsUnique();
        b.HasIndex(s => s.UserId);
        b.HasIndex(s => s.StripeCustomerId);
        b.Property(s => s.Status).HasMaxLength(30);
        b.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
