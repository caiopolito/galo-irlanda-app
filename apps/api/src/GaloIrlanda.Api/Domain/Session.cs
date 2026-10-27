using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Sessão do cookie de login, guardada no servidor para poder ser revogada (ADR 0014).</summary>
public sealed class Session
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid UserId { get; init; }
    public required byte[] TokenHash { get; init; }
    public required byte[] Ticket { get; set; }
    public string? UserAgent { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> b)
    {
        b.HasIndex(s => s.TokenHash).IsUnique();
        b.HasIndex(s => s.UserId).HasFilter("revoked_at IS NULL");
        b.Property(s => s.UserAgent).HasMaxLength(200);
        b.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
