using GaloIrlanda.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Resposta do sócio a um evento: "Vou", "Talvez", "Não vou". Não entra no histórico (GDPR).</summary>
public sealed class EventRsvp
{
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public RsvpAnswer Answer { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum RsvpAnswer { Going, Maybe, NotGoing }

internal sealed class EventRsvpConfiguration : IEntityTypeConfiguration<EventRsvp>
{
    public void Configure(EntityTypeBuilder<EventRsvp> b)
    {
        b.HasKey(r => new { r.EventId, r.UserId });
        b.Property(r => r.Answer).HasTextConversion();
        b.HasOne<Event>().WithMany().HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(r => r.UserId);
        b.ToTable(t => t.HasCheckConstraint("ck_event_rsvps_answer", EnumText<RsvpAnswer>.CheckSql("answer")));
    }
}
