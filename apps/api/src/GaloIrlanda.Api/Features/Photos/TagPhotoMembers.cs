using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Photos;

/// <summary>
/// Marca sócios numa foto, à mão (sem reconhecimento facial, GDPR art. 9). Só entra quem aceitou ser
/// marcado no perfil; os outros são ignorados.
/// </summary>
public static class TagPhotoMembers
{
    public sealed record Tagged(Guid UserId, int MemberNumber, string FullName);

    public sealed record Request(IReadOnlyList<Guid> UserIds);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.UserIds).Must(ids => ids.Count <= 100);
    }

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{id:guid}/tags", List).WithName("ListPhotoTags").WithSummary("Sócios marcados na foto");
        group.MapPut("/{id:guid}/tags", Handle).WithName(nameof(TagPhotoMembers)).WithSummary("Define os sócios marcados na foto")
            .WithValidation<Request>();
    }

    private static async Task<Ok<List<Tagged>>> List(Guid id, AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.PhotoTags.Where(t => t.PhotoId == id)
            .Join(db.Members, t => t.UserId, m => m.UserId, (t, m) => m)
            .OrderBy(m => m.FullName) // ordenar antes de projetar: o EF não traduz propriedade de record construído
            .Select(m => new Tagged(m.UserId, m.MemberNumber, m.FullName))
            .ToListAsync(ct));

    private static async Task<Results<Ok<List<Tagged>>, NotFound>> Handle(
        Guid id, Request request, AppDbContext db, CurrentUser currentUser, TimeProvider clock, CancellationToken ct)
    {
        if (!await db.Photos.AnyAsync(p => p.Id == id && p.DeletedAt == null, ct)) return TypedResults.NotFound();

        var requested = request.UserIds.ToArray();
        var allowed = await db.Members.Where(m => requested.Contains(m.UserId) && m.PhotoTagOptIn)
            .Select(m => m.UserId).ToListAsync(ct);
        var current = await db.PhotoTags.Where(t => t.PhotoId == id).ToListAsync(ct);

        db.PhotoTags.RemoveRange(current.Where(t => !allowed.Contains(t.UserId)));
        db.PhotoTags.AddRange(allowed.Where(u => current.All(t => t.UserId != u)).Select(u => new PhotoTag
        {
            PhotoId = id, UserId = u, TaggedBy = currentUser.Id, CreatedAt = clock.GetUtcNow(),
        }));
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok((await List(id, db, ct)).Value!);
    }
}
