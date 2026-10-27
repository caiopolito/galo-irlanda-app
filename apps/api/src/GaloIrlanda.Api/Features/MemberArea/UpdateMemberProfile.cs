using FluentValidation;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>O sócio edita o próprio perfil. Desligar a marcação em fotos remove as marcações existentes.</summary>
public static class UpdateMemberProfile
{
    public sealed record Request(string FullName, string? Phone, string CityCode, bool PhotoTagOptIn, uint Version);

    public sealed record Response(uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
            RuleFor(x => x.Phone).MaximumLength(30);
            RuleFor(x => x.CityCode).NotEmpty().Length(3);
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/me", Handle).WithName(nameof(UpdateMemberProfile)).WithSummary("Edita o perfil do sócio")
            .WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<Response>, NotFound, ValidationProblem>> Handle(
        Request request, CurrentUser currentUser, AppDbContext db, CancellationToken ct)
    {
        var userId = currentUser.Id!.Value;
        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == userId, ct);
        if (member is null) return TypedResults.NotFound();

        var cityCode = request.CityCode.ToUpperInvariant();
        if (!await db.Cities.AnyAsync(c => c.Code == cityCode, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["cityCode"] = ["Cidade não encontrada."] });

        db.Entry(member).Property(m => m.Version).OriginalValue = request.Version;
        member.FullName = request.FullName.Trim();
        member.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        member.CityCode = cityCode;
        member.PhotoTagOptIn = request.PhotoTagOptIn;
        if (!request.PhotoTagOptIn)
        {
            db.PhotoTags.RemoveRange(await db.PhotoTags.Where(t => t.UserId == userId).ToListAsync(ct));
        }
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new Response(member.Version));
    }
}
