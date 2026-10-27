using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

public sealed class City : AuditableEntity, IPublicContent
{
    /// <summary>Código de três letras ("DUB", "ORK", "GWY"), chave natural.</summary>
    public required string Code { get; init; }
    public required string Name { get; set; }
    public string? Blurb { get; set; }
    public Guid? PhotoId { get; set; }
    public Photo? Photo { get; set; }
    public short SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public uint Version { get; private set; }

    bool IPublicContent.IsPublic => IsActive;
}

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> b)
    {
        b.ConfigureAudit();
        b.HasKey(c => c.Code);
        b.Property(c => c.Code).HasColumnType("char(3)");
        b.Property(c => c.Name).HasMaxLength(60);
        b.Property(c => c.Blurb).HasMaxLength(400);
        b.Property(c => c.Version).IsRowVersion();
        b.HasOne(c => c.Photo).WithMany().HasForeignKey(c => c.PhotoId).OnDelete(DeleteBehavior.SetNull);
        b.ToTable(t => t.HasCheckConstraint("ck_cities_code", "code ~ '^[A-Z]{3}$'"));
    }
}
