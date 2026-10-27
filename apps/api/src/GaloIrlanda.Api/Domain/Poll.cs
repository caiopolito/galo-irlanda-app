using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>
/// Votação da torcida. Em votação secreta, <see cref="PollParticipation"/> registra quem votou e
/// <see cref="PollBallot"/> guarda o voto sem o usuário: não dá para ligar um ao outro.
/// </summary>
public sealed class Poll : AuditableEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool IsSecret { get; set; } = true;
    public DateTimeOffset OpensAt { get; set; }
    public DateTimeOffset ClosesAt { get; set; }
    public bool IsPublished { get; set; }
    /// <summary>Anotação à mão do layout ("se chover, é no pub mesmo").</summary>
    public string? HandNote { get; set; }
    public List<PollOption> Options { get; } = [];
    public uint Version { get; private set; }

    public bool IsOpen(DateTimeOffset now) => IsPublished && OpensAt <= now && now < ClosesAt;
}

public sealed class PollOption
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid PollId { get; init; }
    public required string Label { get; set; }
    public short SortOrder { get; set; }
}

public sealed class PollParticipation
{
    public Guid PollId { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset VotedAt { get; init; }
}

public sealed class PollBallot
{
    /// <summary>UUID v4 (aleatório) de propósito: um v7 revelaria a hora do voto e permitiria cruzar com a participação.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid PollId { get; init; }
    public Guid OptionId { get; init; }
    /// <summary>Só preenchido em votação aberta.</summary>
    public Guid? UserId { get; init; }
}

internal sealed class PollConfiguration : IEntityTypeConfiguration<Poll>
{
    public void Configure(EntityTypeBuilder<Poll> b)
    {
        b.ConfigureAudit();
        b.Property(p => p.Title).HasMaxLength(160);
        b.Property(p => p.Description).HasMaxLength(1000);
        b.Property(p => p.HandNote).HasMaxLength(80);
        b.Property(p => p.Version).IsRowVersion();
        b.HasMany(p => p.Options).WithOne().HasForeignKey(o => o.PollId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t => t.HasCheckConstraint("ck_polls_period", "closes_at > opens_at"));
    }
}

internal sealed class PollOptionConfiguration : IEntityTypeConfiguration<PollOption>
{
    public void Configure(EntityTypeBuilder<PollOption> b) => b.Property(o => o.Label).HasMaxLength(120);
}

internal sealed class PollParticipationConfiguration : IEntityTypeConfiguration<PollParticipation>
{
    public void Configure(EntityTypeBuilder<PollParticipation> b)
    {
        b.HasKey(p => new { p.PollId, p.UserId });
        b.HasOne<Poll>().WithMany().HasForeignKey(p => p.PollId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PollBallotConfiguration : IEntityTypeConfiguration<PollBallot>
{
    public void Configure(EntityTypeBuilder<PollBallot> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasOne<Poll>().WithMany().HasForeignKey(x => x.PollId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<PollOption>().WithMany().HasForeignKey(x => x.OptionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.PollId);
    }
}
