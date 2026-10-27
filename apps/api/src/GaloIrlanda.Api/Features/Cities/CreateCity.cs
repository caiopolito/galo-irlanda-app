using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Cities;

/// <summary>Nova cidade (um novo "portão"). Código de três letras, como os de aeroporto: DUB, ORK, GWY.</summary>
public static class CreateCity
{
    public sealed record Request(string Code, string Name, string? Blurb, short SortOrder);

    public sealed record Response(string Code, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().Matches("^[A-Za-z]{3}$").WithMessage("Use três letras, como DUB.");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
            RuleFor(x => x.Blurb).MaximumLength(400);
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/", Handle).WithName(nameof(CreateCity)).WithSummary("Cria uma cidade").WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<Created<Response>> Handle(Request request, AppDbContext db, CancellationToken ct)
    {
        var city = new City
        {
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name.Trim(),
            Blurb = request.Blurb,
            SortOrder = request.SortOrder,
        };
        db.Cities.Add(city);
        await db.SaveChangesAsync(ct); // código repetido vira 409 no DbExceptionHandler

        return TypedResults.Created($"/api/admin/cities/{city.Code}", new Response(city.Code, city.Version));
    }
}
