using GaloIrlanda.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

public sealed class User : ITimestamped
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? ErasedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<UserRole> Roles { get; } = [];
    public uint Version { get; private set; }
}

public enum UserStatus { Active, Disabled, Erased }

public sealed class UserRole
{
    public Guid UserId { get; init; }
    public required string Role { get; init; }
    public DateTimeOffset GrantedAt { get; init; }
    public Guid? GrantedBy { get; init; }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(u => u.Email).HasColumnType("citext");
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.DisplayName).HasMaxLength(80);
        b.Property(u => u.Status).HasTextConversion();
        b.Property(u => u.Version).IsRowVersion();
        b.HasMany(u => u.Roles).WithOne().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_users_status", EnumText<UserStatus>.CheckSql("status"));
            t.HasCheckConstraint("ck_users_erased", "(status = 'erased') = (erased_at IS NOT NULL)");
        });
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.HasKey(r => new { r.UserId, r.Role });
        b.HasOne<User>().WithMany().HasForeignKey(r => r.GrantedBy).OnDelete(DeleteBehavior.SetNull);
        b.ToTable(t => t.HasCheckConstraint("ck_user_roles_role", "role IN ('admin', 'editor', 'socio')"));
    }
}
