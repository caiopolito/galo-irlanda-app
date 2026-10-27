using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>Documentos publicados para sócios. O arquivo é entregue pela API, só para quem está logado.</summary>
public static class MemberDocuments
{
    public sealed record Item(Guid Id, string Title, string FileName, long SizeBytes);

    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/documents", Handle).WithName("ListMemberDocuments").WithSummary("Documentos da torcida");
        group.MapGet("/documents/{id:guid}/download", Download).WithName("DownloadMemberDocument")
            .WithSummary("Baixa um documento");
    }

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Documents.Where(d => d.IsPublished).OrderBy(d => d.SortOrder).ThenBy(d => d.Title)
            .Select(d => new Item(d.Id, d.Title, d.FileName, d.SizeBytes)).ToListAsync(ct));

    private static async Task<Results<FileStreamHttpResult, NotFound>> Download(
        Guid id, AppDbContext db, IObjectStorage storage, CancellationToken ct)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.IsPublished, ct);
        if (document is null || await storage.OpenReadAsync(document.StorageKey, ct) is not { } stream)
            return TypedResults.NotFound();

        return TypedResults.File(stream, document.ContentType, document.FileName);
    }
}
