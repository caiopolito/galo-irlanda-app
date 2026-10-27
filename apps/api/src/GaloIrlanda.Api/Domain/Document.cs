using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Documento só para sócios (estatuto, atas, regras). O arquivo é entregue pela API, nunca por URL pública.</summary>
public sealed class Document : AuditableEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Title { get; set; }
    public required string StorageKey { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long SizeBytes { get; init; }
    public short SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public uint Version { get; private set; }
}

internal sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> b)
    {
        b.ConfigureAudit();
        b.Property(d => d.Title).HasMaxLength(120);
        b.Property(d => d.StorageKey).HasMaxLength(200);
        b.HasIndex(d => d.StorageKey).IsUnique();
        b.Property(d => d.FileName).HasMaxLength(200);
        b.Property(d => d.ContentType).HasMaxLength(100);
        b.Property(d => d.Version).IsRowVersion();
    }
}
