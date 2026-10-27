using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Sócio marcado numa foto, à mão (sem reconhecimento facial). Só para quem aceitou ser marcado.</summary>
public sealed class PhotoTag
{
    public Guid PhotoId { get; init; }
    public Guid UserId { get; init; }
    public Guid? TaggedBy { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

internal sealed class PhotoTagConfiguration : IEntityTypeConfiguration<PhotoTag>
{
    public void Configure(EntityTypeBuilder<PhotoTag> b)
    {
        b.HasKey(t => new { t.PhotoId, t.UserId });
        b.HasOne<Photo>().WithMany().HasForeignKey(t => t.PhotoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(t => t.TaggedBy).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(t => t.UserId);
    }
}
