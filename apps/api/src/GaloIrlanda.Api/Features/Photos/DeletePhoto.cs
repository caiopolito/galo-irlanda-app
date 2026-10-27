using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Photos;

/// <summary>
/// Tira a foto do ar na hora e a coloca na fila de purga: o cron apaga os arquivos e depois a linha,
/// para o banco nunca apontar para um arquivo que já sumiu.
/// </summary>
public static class DeletePhoto
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapDelete("/{id:guid}", Handle).WithName(nameof(DeletePhoto)).WithSummary("Exclui uma foto");

    private static async Task<Results<NoContent, NotFound>> Handle(
        Guid id, AppDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var photo = await db.Photos.FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, ct);
        if (photo is null) return TypedResults.NotFound();

        photo.DeletedAt = clock.GetUtcNow();
        photo.Visibility = PhotoVisibility.Private;
        photo.WallFeatured = false;
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
