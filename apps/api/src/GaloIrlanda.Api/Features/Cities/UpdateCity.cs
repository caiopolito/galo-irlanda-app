using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Cities;

public static class UpdateCity
{
    public sealed record Request(string Name, string? Blurb, Guid? PhotoId, short SortOrder, bool IsActive, uint Version);

    public sealed record Response(string Code, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
            RuleFor(x => x.Blurb).MaximumLength(400);
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPut("/{code}", Handle).WithName(nameof(UpdateCity)).WithSummary("Edita uma cidade").WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Results<Ok<Response>, NotFound, ValidationProblem>> Handle(
        string code, Request request, AppDbContext db, CancellationToken ct)
    {
        var city = await db.Cities.FirstOrDefaultAsync(c => c.Code == code.ToUpperInvariant(), ct);
        if (city is null) return TypedResults.NotFound();

        if (request.PhotoId is { } photoId && !await db.Photos.AnyAsync(p => p.Id == photoId && p.DeletedAt == null, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["photoId"] = ["Foto não encontrada."] });

        db.Entry(city).Property(c => c.Version).OriginalValue = request.Version;
        city.Name = request.Name.Trim();
        city.Blurb = request.Blurb;
        city.PhotoId = request.PhotoId;
        city.SortOrder = request.SortOrder;
        city.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(new Response(city.Code, city.Version));
    }
}
