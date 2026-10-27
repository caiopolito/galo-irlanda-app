using GaloIrlanda.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Notícia da seção "Da Torcida". Agendado = <c>Published</c> com <c>PublishedAt</c> no futuro.</summary>
public sealed class Post : AuditableEntity, IPublicContent
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Slug { get; init; }
    public required string Title { get; set; }
    public string? Excerpt { get; set; }
    public string BodyMd { get; set; } = "";
    public short? CategoryId { get; set; }
    public PostCategory? Category { get; set; }
    public Guid? CoverPhotoId { get; set; }
    public Photo? CoverPhoto { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Draft;
    public DateTimeOffset? PublishedAt { get; set; }
    public Guid? AuthorId { get; set; }
    public uint Version { get; private set; }

    bool IPublicContent.IsPublic => Status == PostStatus.Published;
}

public enum PostStatus { Draft, Published, Archived }

public sealed class PostCategory
{
    public short Id { get; init; }
    public required string Slug { get; set; }
    public required string Name { get; set; }
    public short SortOrder { get; set; }
}

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> b)
    {
        b.ConfigureAudit();
        b.HasIndex(p => p.Slug).IsUnique();
        b.HasIndex(p => p.PublishedAt).IsDescending().HasFilter("status = 'published'");
        b.Property(p => p.Title).HasMaxLength(160);
        b.Property(p => p.Excerpt).HasMaxLength(300);
        b.Property(p => p.Status).HasTextConversion();
        b.Property(p => p.Version).IsRowVersion();
        b.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(p => p.CoverPhoto).WithMany().HasForeignKey(p => p.CoverPhotoId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.SetNull);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_posts_status", EnumText<PostStatus>.CheckSql("status"));
            t.HasCheckConstraint("ck_posts_slug", "slug ~ '^[a-z0-9]+(-[a-z0-9]+)*$'");
            t.HasCheckConstraint("ck_posts_published_at", "status <> 'published' OR published_at IS NOT NULL");
        });
    }
}

internal sealed class PostCategoryConfiguration : IEntityTypeConfiguration<PostCategory>
{
    public void Configure(EntityTypeBuilder<PostCategory> b)
    {
        b.Property(c => c.Id).UseIdentityAlwaysColumn();
        b.HasIndex(c => c.Slug).IsUnique();
        b.HasIndex(c => c.Name).IsUnique();
        b.Property(c => c.Name).HasMaxLength(40);
    }
}
