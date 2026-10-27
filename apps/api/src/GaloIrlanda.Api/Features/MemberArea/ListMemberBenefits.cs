using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>Benefícios ativos, na ordem definida no admin.</summary>
public static class ListMemberBenefits
{
    public sealed record Item(Guid Id, string Headline, string Title, string? Description, bool Highlight);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/benefits", Handle).WithName(nameof(ListMemberBenefits)).WithSummary("Benefícios do sócio");

    private static async Task<Ok<List<Item>>> Handle(AppDbContext db, CancellationToken ct) =>
        TypedResults.Ok(await db.Benefits.Where(b => b.IsActive).OrderBy(b => b.SortOrder)
            .Select(b => new Item(b.Id, b.Headline, b.Title, b.Description, b.Highlight)).ToListAsync(ct));
}
