using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Posts;

/// <summary>Edita o conteúdo. O slug não muda: o link já pode ter sido compartilhado.</summary>
public static class UpdatePost
{
    public sealed record Request(
        string Title, string? Excerpt, string BodyMd, short? CategoryId, Guid? CoverPhotoId, uint Version);

    public sealed record Response(Guid Id, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
            RuleFor(x => x.Excerpt).MaximumLength(300);
            RuleFor(x => x.BodyMd).NotNull();
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{id:guid}", Handle)
            .WithName(nameof(UpdatePost))
            .WithSummary("Edita uma notícia")
            .WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<Response>, NotFound, ValidationProblem>> Handle(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var post = await db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post is null) return TypedResults.NotFound();

        if (await PostReferences.ValidateAsync(db, request.CategoryId, request.CoverPhotoId, ct) is { } problem)
            return problem;

        db.Entry(post).Property(p => p.Version).OriginalValue = request.Version;
        post.Title = request.Title.Trim();
        post.Excerpt = request.Excerpt;
        post.BodyMd = request.BodyMd;
        post.CategoryId = request.CategoryId;
        post.CoverPhotoId = request.CoverPhotoId;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new Response(post.Id, post.Version));
    }
}
