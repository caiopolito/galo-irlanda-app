using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Local de evento: pub parceiro, parque, casa de alguém.</summary>
public sealed class Venue : AuditableEntity, IPublicContent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string CityCode { get; set; }
    public City City { get; set; } = null!;
    public required string Name { get; set; }
    public string? Neighborhood { get; set; }
    public string? Address { get; set; }
    public string? MapsUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsPartner { get; set; }
    public bool? ShowsWithSound { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; private set; }

    bool IPublicContent.IsPublic => IsActive;
}

internal sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> b)
    {
        b.ConfigureAudit();
        b.Property(v => v.CityCode).HasColumnType("char(3)");
        b.Property(v => v.Name).HasMaxLength(120);
        b.Property(v => v.Version).IsRowVersion();
        b.HasOne(v => v.City).WithMany().HasForeignKey(v => v.CityCode).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(v => new { v.CityCode, v.Name }).IsUnique();
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_venues_maps_url", "maps_url ~ '^https://'");
            t.HasCheckConstraint("ck_venues_website_url", "website_url ~ '^https://'");
        });
    }
}
