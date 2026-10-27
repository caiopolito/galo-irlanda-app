using GaloIrlanda.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

public sealed class Photo : AuditableEntity, IPublicContent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public PhotoUploadStatus UploadStatus { get; set; } = PhotoUploadStatus.Pending;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public string? Caption { get; set; }
    public DateOnly? TakenOn { get; set; }
    public Guid? EventId { get; set; }
    public PhotoVisibility Visibility { get; set; } = PhotoVisibility.Private;
    public ConsentBasis ConsentBasis { get; set; } = ConsentBasis.Pending;
    public string? ConsentNote { get; set; }
    public Guid? ConsentConfirmedBy { get; set; }
    public DateTimeOffset? ConsentConfirmedAt { get; set; }
    public bool WallFeatured { get; set; }
    public int? WallSortOrder { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public uint Version { get; private set; }

    bool IPublicContent.IsPublic => Visibility == PhotoVisibility.Public && DeletedAt is null;
}

public enum PhotoUploadStatus { Pending, Ready }

public enum PhotoVisibility { Public, Members, Private }

public enum ConsentBasis { Pending, NoIdentifiablePeople, ConsentRecorded, Withdrawn }

internal sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> b)
    {
        b.ConfigureAudit();
        b.Property(p => p.UploadStatus).HasTextConversion();
        b.Property(p => p.Visibility).HasTextConversion();
        b.Property(p => p.ConsentBasis).HasTextConversion();
        b.Property(p => p.AltText).HasMaxLength(300);
        b.Property(p => p.Title).HasMaxLength(120);
        b.Property(p => p.Caption).HasMaxLength(300);
        b.Property(p => p.ConsentNote).HasMaxLength(500);
        b.Property(p => p.Version).IsRowVersion();
        b.HasOne<Event>().WithMany().HasForeignKey(p => p.EventId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(p => p.ConsentConfirmedBy).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(p => p.WallSortOrder).HasFilter("wall_featured AND deleted_at IS NULL");
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_photos_upload_status", EnumText<PhotoUploadStatus>.CheckSql("upload_status"));
            t.HasCheckConstraint("ck_photos_visibility", EnumText<PhotoVisibility>.CheckSql("visibility"));
            t.HasCheckConstraint("ck_photos_consent_basis", EnumText<ConsentBasis>.CheckSql("consent_basis"));
            t.HasCheckConstraint("ck_photos_dimensions", "(width IS NULL OR width > 0) AND (height IS NULL OR height > 0)");
            t.HasCheckConstraint("ck_photos_public_ok",
                "visibility <> 'public' OR (upload_status = 'ready' AND alt_text IS NOT NULL " +
                "AND consent_basis IN ('no_identifiable_people', 'consent_recorded'))");
            t.HasCheckConstraint("ck_photos_wall", "NOT wall_featured OR visibility = 'public'");
            t.HasCheckConstraint("ck_photos_consent",
                "consent_basis NOT IN ('consent_recorded', 'no_identifiable_people') OR consent_confirmed_at IS NOT NULL");
        });
    }
}
