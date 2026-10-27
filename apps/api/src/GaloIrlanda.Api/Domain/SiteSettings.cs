using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Configurações do site: linha única (<c>Id</c> sempre <c>true</c>).</summary>
public sealed class SiteSettings : ITimestamped, IPublicContent
{
    public bool Id { get; init; } = true;
    public Guid? HeroPhotoId { get; set; }
    public Photo? HeroPhoto { get; set; }
    public int? MembershipFeeCents { get; set; }
    public string MembershipCurrency { get; set; } = "EUR";
    public string? WhatsappUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? ContactEmail { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint Version { get; private set; }

    bool IPublicContent.IsPublic => true;
}

internal sealed class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> b)
    {
        b.Property(s => s.Id).ValueGeneratedNever();
        b.Property(s => s.MembershipCurrency).HasColumnType("char(3)");
        b.Property(s => s.ContactEmail).HasColumnType("citext");
        b.Property(s => s.Version).IsRowVersion();
        b.HasOne(s => s.HeroPhoto).WithMany().HasForeignKey(s => s.HeroPhotoId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(s => s.UpdatedBy).OnDelete(DeleteBehavior.SetNull);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_site_settings_single_row", "id");
            t.HasCheckConstraint("ck_site_settings_fee", "membership_fee_cents IS NULL OR membership_fee_cents >= 0");
            t.HasCheckConstraint("ck_site_settings_whatsapp", "whatsapp_url ~ '^https://'");
            t.HasCheckConstraint("ck_site_settings_instagram", "instagram_url ~ '^https://'");
        });
    }
}
