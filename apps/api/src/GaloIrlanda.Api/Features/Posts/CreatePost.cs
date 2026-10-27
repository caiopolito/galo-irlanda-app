using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Text;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Posts;

/// <summary>Cria a notícia como rascunho. Para ir ao ar, use <see cref="PublishPost"/>.</summary>
public static class CreatePost
{
    public sealed record Request(string Title, string? Excerpt, string BodyMd, short? CategoryId, Guid? CoverPhotoId);

    public sealed record Response(Guid Id, string Slug, uint Version);

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
        group.MapPost("/", Handle)
            .WithName(nameof(CreatePost))
            .WithSummary("Cria uma notícia (rascunho)")
            .WithValidation<Request>();

    private static async Task<Results<Created<Response>, ValidationProblem>> Handle(
        Request request, AppDbContext db, CurrentUser currentUser, CancellationToken ct)
    {
        if (await PostReferences.ValidateAsync(db, request.CategoryId, request.CoverPhotoId, ct) is { } problem)
            return problem;

        var post = new Post
        {
            Slug = Slug.From(request.Title),
            Title = request.Title.Trim(),
            Excerpt = request.Excerpt,
            BodyMd = request.BodyMd,
            CategoryId = request.CategoryId,
            CoverPhotoId = request.CoverPhotoId,
            AuthorId = currentUser.Id,
        };
        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/admin/posts/{post.Id}", new Response(post.Id, post.Slug, post.Version));
    }
}

/// <summary>Checagens de categoria e capa usadas ao criar e ao editar uma notícia.</summary>
internal static class PostReferences
{
    public static async Task<ValidationProblem?> ValidateAsync(
        AppDbContext db, short? categoryId, Guid? coverPhotoId, CancellationToken ct)
    {
        if (categoryId is { } c && !await db.PostCategories.AnyAsync(x => x.Id == c, ct))
            return Problem("categoryId", "Categoria não encontrada.");
        if (coverPhotoId is { } p && !await db.Photos.AnyAsync(x => x.Id == p && x.DeletedAt == null, ct))
            return Problem("coverPhotoId", "Foto não encontrada.");
        return null;
    }

    private static ValidationProblem Problem(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
