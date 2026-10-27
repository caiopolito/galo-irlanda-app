using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Documents;

/// <summary>Lista, edita (título, ordem, publicação), baixa e exclui documentos no admin.</summary>
public static class ManageDocuments
{
    public sealed record Item(
        Guid Id, string Title, string FileName, long SizeBytes, short SortOrder, bool IsPublished, DateTimeOffset UpdatedAt,
        uint Version);

    public sealed record UpdateRequest(string Title, short SortOrder, bool IsPublished, uint Version);

    public sealed class UpdateValidator : AbstractValidator<UpdateRequest>
    {
        public UpdateValidator() => RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
    }

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/", List).WithName("ListDocuments").WithSummary("Lista os documentos");
        group.MapPut("/{id:guid}", Update).WithName("UpdateDocument").WithSummary("Edita um documento")
            .WithValidation<UpdateRequest>()
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/{id:guid}/download", Download).WithName("DownloadDocument").WithSummary("Baixa um documento");
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteDocument").WithSummary("Exclui um documento");
    }

    private static async Task<Ok<List<Item>>> List(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Documents.OrderBy(d => d.SortOrder).ThenBy(d => d.Title)
            .Select(d => new Item(d.Id, d.Title, d.FileName, d.SizeBytes, d.SortOrder, d.IsPublished, d.UpdatedAt, d.Version))
            .ToListAsync(ct));

    private static async Task<Results<NoContent, NotFound>> Update(
        Guid id, UpdateRequest request, AppDbContext db, CancellationToken ct)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (document is null) return TypedResults.NotFound();

        db.Entry(document).Property(d => d.Version).OriginalValue = request.Version;
        document.Title = request.Title.Trim();
        document.SortOrder = request.SortOrder;
        document.IsPublished = request.IsPublished;
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static async Task<Results<FileStreamHttpResult, NotFound>> Download(
        Guid id, AppDbContext db, IObjectStorage storage, CancellationToken ct)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (document is null || await storage.OpenReadAsync(document.StorageKey, ct) is not { } stream)
            return TypedResults.NotFound();
        return TypedResults.File(stream, document.ContentType, document.FileName);
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        Guid id, AppDbContext db, IObjectStorage storage, CancellationToken ct)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (document is null) return TypedResults.NotFound();

        db.Documents.Remove(document);
        await db.SaveChangesAsync(ct);
        await storage.DeleteAsync([document.StorageKey], ct);
        return TypedResults.NoContent();
    }
}
