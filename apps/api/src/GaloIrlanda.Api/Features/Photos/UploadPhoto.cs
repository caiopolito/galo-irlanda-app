using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Photos;

/// <summary>
/// Recebe a foto (multipart), gera as variantes sem metadados e cria a foto como privada.
/// Legenda, consentimento e publicação vêm depois, em <see cref="UpdatePhoto"/>.
/// </summary>
public static class UploadPhoto
{
    private static readonly string[] AcceptedTypes = ["image/jpeg", "image/png", "image/webp"];

    public sealed record Response(Guid Id, string BaseUrl, int Width, int Height, uint Version);

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle)
            .WithName(nameof(UploadPhoto))
            .WithSummary("Envia uma foto (JPEG, PNG ou WebP, até 20 MB)")
            .DisableAntiforgery() // CSRF é coberto pelo header X-Galo-Csrf
            .ProducesValidationProblem();

    private static async Task<Results<Created<Response>, ValidationProblem>> Handle(
        IFormFile file, AppDbContext db, PhotoProcessor processor, IObjectStorage storage, CancellationToken ct)
    {
        if (!AcceptedTypes.Contains(file.ContentType))
            return Invalid("Envie uma foto em JPEG, PNG ou WebP.");
        if (file.Length is 0 or > PhotoProcessor.MaxBytes)
            return Invalid("A foto precisa ter até 20 MB.");

        var photo = new Photo();
        await using (var input = file.OpenReadStream())
        {
            if (await processor.ProcessAsync(photo.Id, input, ct) is not var (width, height))
                return Invalid("Não foi possível ler a imagem.");

            photo.Width = width;
            photo.Height = height;
            photo.UploadStatus = PhotoUploadStatus.Ready;
        }

        db.Photos.Add(photo);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created(
            $"/api/admin/photos/{photo.Id}",
            new Response(photo.Id, storage.PhotoBaseUrl(photo.Id), photo.Width.Value, photo.Height.Value, photo.Version));
    }

    private static ValidationProblem Invalid(string message) =>
        TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["file"] = [message] });
}
