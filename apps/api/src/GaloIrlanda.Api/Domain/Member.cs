using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Sócio da torcida: perfil e carteirinha de um usuário. A situação da mensalidade vem de <see cref="Subscription"/>.</summary>
public sealed class Member : ITimestamped
{
    public Guid UserId { get; init; }
    public User User { get; init; } = null!;
    /// <summary>Número da carteirinha, de uma sequência; nunca reaproveitado.</summary>
    public int MemberNumber { get; private set; }
    public required string FullName { get; set; }
    public string? Phone { get; set; }
    public required string CityCode { get; set; }
    public DateTimeOffset JoinedAt { get; init; }
    /// <summary>Código do link "indicar amigo".</summary>
    public required string ReferralCode { get; init; }
    public Guid? ReferredBy { get; init; }
    /// <summary>Aceita ser marcado nas fotos (GDPR: marcação manual e opcional).</summary>
    public bool PhotoTagOptIn { get; set; }
    /// <summary>Muda quando a carteirinha é reemitida: invalida QR codes antigos.</summary>
    public short CardKeyVersion { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public uint Version { get; private set; }
}

internal sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public const string NumberSequence = "member_number_seq";

    public void Configure(EntityTypeBuilder<Member> b)
    {
        b.HasKey(m => m.UserId);
        b.HasOne(m => m.User).WithOne().HasForeignKey<Member>(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Property(m => m.MemberNumber).HasDefaultValueSql($"nextval('{NumberSequence}')").ValueGeneratedOnAdd();
        b.HasIndex(m => m.MemberNumber).IsUnique();
        b.Property(m => m.FullName).HasMaxLength(120);
        b.Property(m => m.Phone).HasMaxLength(30);
        b.Property(m => m.CityCode).HasColumnType("char(3)");
        b.HasOne<City>().WithMany().HasForeignKey(m => m.CityCode).OnDelete(DeleteBehavior.Restrict);
        b.Property(m => m.ReferralCode).HasMaxLength(12);
        b.HasIndex(m => m.ReferralCode).IsUnique();
        b.HasOne<User>().WithMany().HasForeignKey(m => m.ReferredBy).OnDelete(DeleteBehavior.SetNull);
        b.Property(m => m.Version).IsRowVersion();
    }
}
