using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Members;

/// <summary>Reemite a carteirinha (ex.: celular perdido): QR codes antigos deixam de valer.</summary>
public static class ReissueMemberCard
{
    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/{userId:guid}/reissue-card", Handle).WithName(nameof(ReissueMemberCard))
            .WithSummary("Invalida a carteirinha atual e emite outra");

    private static async Task<Results<NoContent, NotFound>> Handle(Guid userId, AppDbContext db, CancellationToken ct)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (member is null) return TypedResults.NotFound();

        member.CardKeyVersion++;
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
