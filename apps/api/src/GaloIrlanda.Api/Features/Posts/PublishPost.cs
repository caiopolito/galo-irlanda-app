using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Posts;

/// <summary>
/// Publica agora ou agenda (<c>publishAt</c> no futuro). Agendado é só "publicado com data futura":
/// o cron regenera o site quando a hora chega. <c>unpublish</c> volta para rascunho; <c>archive</c> tira do ar.
/// </summary>
public static class PublishPost
{
    public sealed record Request(DateTimeOffset? PublishAt);

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/{id:guid}/publish", Publish).WithName(nameof(PublishPost)).WithSummary("Publica ou agenda uma notícia");
        group.MapPost("/{id:guid}/unpublish", (Guid id, AppDbContext db, CancellationToken ct) => SetStatus(id, PostStatus.Draft, db, ct))
            .WithName("UnpublishPost").WithSummary("Volta a notícia para rascunho");
        group.MapPost("/{id:guid}/archive", (Guid id, AppDbContext db, CancellationToken ct) => SetStatus(id, PostStatus.Archived, db, ct))
            .WithName("ArchivePost").WithSummary("Arquiva a notícia (sai do site)");
    }

    private static async Task<Results<NoContent, NotFound>> Publish(
        Guid id, Request request, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null) return TypedResults.NotFound();

        post.Status = PostStatus.Published;
        post.PublishedAt = request.PublishAt?.ToUniversalTime() ?? clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> SetStatus(
        Guid id, PostStatus status, AppDbContext db, CancellationToken ct)
    {
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null) return TypedResults.NotFound();

        post.Status = status;
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
