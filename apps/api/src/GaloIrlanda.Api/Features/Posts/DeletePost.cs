using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Posts;

/// <summary>Exclusão definitiva, só para administradores. Para só tirar do site, arquive.</summary>
public static class DeletePost
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:guid}", Handle)
            .WithName(nameof(DeletePost))
            .WithSummary("Exclui uma notícia")
            .RequireAuthorization(Policies.Admin);

    private static async Task<Results<NoContent, NotFound>> Handle(Guid id, AppDbContext db, CancellationToken ct)
    {
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null) return TypedResults.NotFound();

        db.Posts.Remove(post);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
