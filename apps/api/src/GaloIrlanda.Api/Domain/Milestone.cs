using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Marco da seção "Escalas". Só vai ao ar depois de conferir a fonte.</summary>
public sealed class Milestone : AuditableEntity, IPublicContent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public short Year { get; set; }
    public required string Text { get; set; }
    public Guid? PhotoId { get; set; }
    public short SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public uint Version { get; private set; }

    bool IPublicContent.IsPublic => IsPublished;
}

internal sealed class MilestoneConfiguration : IEntityTypeConfiguration<Milestone>
{
    public void Configure(EntityTypeBuilder<Milestone> b)
    {
        b.ConfigureAudit();
        b.Property(m => m.Text).HasMaxLength(200);
        b.Property(m => m.Version).IsRowVersion();
        b.HasOne<Photo>().WithMany().HasForeignKey(m => m.PhotoId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(m => new { m.Year, m.SortOrder }).HasFilter("is_published");
        b.ToTable(t => t.HasCheckConstraint("ck_milestones_year", "year BETWEEN 1900 AND 2100"));
    }
}
