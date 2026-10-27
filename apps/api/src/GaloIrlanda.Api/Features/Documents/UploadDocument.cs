using GaloIrlanda.Api.Common.Text;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GaloIrlanda.Api.Features.Documents;

/// <summary>Envia um documento para os sócios (PDF, Word ou imagem). Nasce despublicado.</summary>
public static class UploadDocument
{
    private const long MaxBytes = 20 * 1024 * 1024;

    private static readonly string[] AcceptedTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "image/jpeg",
        "image/png",
    ];

    public sealed record Response(Guid Id, uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle).WithName(nameof(UploadDocument)).WithSummary("Envia um documento (até 20 MB)")
            .DisableAntiforgery()
            .ProducesValidationProblem();

    private static async Task<Results<Created<Response>, ValidationProblem>> Handle(
        IFormFile file, [FromForm] string title, AppDbContext db, IObjectStorage storage, CancellationToken ct)
    {
        if (!AcceptedTypes.Contains(file.ContentType))
            return Invalid("file", "Envie PDF, Word (.docx), JPEG ou PNG.");
        if (file.Length is 0 or > MaxBytes) return Invalid("file", "O arquivo precisa ter até 20 MB.");
        if (string.IsNullOrWhiteSpace(title) || title.Length > 120) return Invalid("title", "Dê um título de até 120 caracteres.");

        var id = Guid.CreateVersion7();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Slug.From(title)}{extension}";
        var key = $"documents/{id}/{fileName}";
        await using (var stream = file.OpenReadStream())
            await storage.PutAsync(key, stream, file.ContentType, ct);

        var document = new Document
        {
            Id = id, Title = title.Trim(), StorageKey = key, FileName = fileName, ContentType = file.ContentType,
            SizeBytes = file.Length,
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/admin/documents/{id}", new Response(id, document.Version));
    }

    private static ValidationProblem Invalid(string field, string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { [field] = [message] });
}
