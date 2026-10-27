using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GaloIrlanda.Api.Domain;

/// <summary>Datas de criação/alteração, preenchidas pelo <c>AuditingInterceptor</c>.</summary>
public interface ITimestamped
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Entidade editada no admin: ganha autor, datas e entra no histórico de alterações.</summary>
public abstract class AuditableEntity : ITimestamped
{
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Conteúdo que aparece no site público: mudar exige regenerar o site.</summary>
public interface IPublicContent
{
    bool IsPublic { get; }
}

internal static class AuditableEntityConfiguration
{
    public static void ConfigureAudit<T>(this EntityTypeBuilder<T> b) where T : AuditableEntity
    {
        b.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedBy).OnDelete(DeleteBehavior.SetNull);
    }
}
