using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Benefício de sócio. No card: <c>Headline</c> grande ("10%", "Mesa"), <c>Title</c> em negrito e <c>Description</c>.</summary>
public sealed class Benefit : AuditableEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Headline { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public Guid? VenueId { get; set; }
    public bool Highlight { get; set; }
    public short SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; private set; }
}

internal sealed class BenefitConfiguration : IEntityTypeConfiguration<Benefit>
{
    public void Configure(EntityTypeBuilder<Benefit> b)
    {
        b.ConfigureAudit();
        b.Property(x => x.Headline).HasMaxLength(20);
        b.Property(x => x.Title).HasMaxLength(80);
        b.Property(x => x.Description).HasMaxLength(200);
        b.HasOne<Venue>().WithMany().HasForeignKey(x => x.VenueId).OnDelete(DeleteBehavior.SetNull);
        b.Property(x => x.Version).IsRowVersion();
    }
}
