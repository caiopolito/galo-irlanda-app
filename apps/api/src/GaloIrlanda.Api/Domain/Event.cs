using GaloIrlanda.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Jogo ou encontro (pelada, churrasco) exibido no painel de partidas.</summary>
public sealed class Event : AuditableEntity, IPublicContent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Slug { get; init; }
    public EventKind Kind { get; set; }
    public required string Title { get; set; }
    public string? Opponent { get; set; }
    public string? Competition { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public required string CityCode { get; set; }
    public City City { get; set; } = null!;
    public Guid? VenueId { get; set; }
    public Venue? Venue { get; set; }
    /// <summary>Local sem cadastro: "Phoenix Park", "[Local a definir]".</summary>
    public string? VenueNote { get; set; }
    public string? HomeTrip { get; set; }
    public string? DescriptionMd { get; set; }
    public Guid? CoverPhotoId { get; set; }
    public Photo? CoverPhoto { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Scheduled;
    public bool IsPublished { get; set; }
    public int? Capacity { get; set; }
    /// <summary>Manual na fase 0; vira contagem de RSVPs na fase 1.</summary>
    public int SpotsTaken { get; set; }
    public uint Version { get; private set; }

    bool IPublicContent.IsPublic => IsPublished;
}

public enum EventKind { Match, Meetup }

/// <summary>Decisão humana. O selo "Embarcando" é derivado no front pelo horário.</summary>
public enum EventStatus { Scheduled, Voting, Confirmed, Postponed, Cancelled }

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> b)
    {
        b.ConfigureAudit();
        b.HasIndex(e => e.Slug).IsUnique();
        b.HasIndex(e => e.StartsAt).HasFilter("is_published");
        b.Property(e => e.Kind).HasTextConversion();
        b.Property(e => e.Status).HasTextConversion();
        b.Property(e => e.Title).HasMaxLength(120);
        b.Property(e => e.Opponent).HasMaxLength(80);
        b.Property(e => e.Competition).HasMaxLength(80);
        b.Property(e => e.VenueNote).HasMaxLength(120);
        b.Property(e => e.HomeTrip).HasMaxLength(200);
        b.Property(e => e.CityCode).HasColumnType("char(3)");
        b.Property(e => e.Version).IsRowVersion();
        b.HasOne(e => e.City).WithMany().HasForeignKey(e => e.CityCode).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.Venue).WithMany().HasForeignKey(e => e.VenueId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(e => e.CoverPhoto).WithMany().HasForeignKey(e => e.CoverPhotoId).OnDelete(DeleteBehavior.SetNull);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_events_kind", EnumText<EventKind>.CheckSql("kind"));
            t.HasCheckConstraint("ck_events_status", EnumText<EventStatus>.CheckSql("status"));
            t.HasCheckConstraint("ck_events_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            t.HasCheckConstraint("ck_events_match_opponent", "kind <> 'match' OR opponent IS NOT NULL");
            t.HasCheckConstraint("ck_events_place", "venue_id IS NOT NULL OR venue_note IS NOT NULL");
            t.HasCheckConstraint("ck_events_capacity", "capacity IS NULL OR capacity > 0");
            t.HasCheckConstraint("ck_events_spots_taken", "spots_taken >= 0");
        });
    }
}
