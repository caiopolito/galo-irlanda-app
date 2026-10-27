using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Benefits;

/// <summary>Cria (POST) ou edita (PUT) um benefício. "Headline" é o texto grande do card ("10%", "Mesa").</summary>
public static class SaveBenefit
{
    public sealed record Request(
        string Headline, string Title, string? Description, Guid? VenueId, bool Highlight, short SortOrder, bool IsActive,
        uint? Version);

    public sealed record Response(Guid Id, uint Version);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Headline).NotEmpty().MaximumLength(20);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(80);
            RuleFor(x => x.Description).MaximumLength(200);
        }
    }

    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/", Create).WithName("CreateBenefit").WithSummary("Cria um benefício").WithValidation<Request>();
        group.MapPut("/{id:guid}", Update).WithName("UpdateBenefit").WithSummary("Edita um benefício").WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:guid}", Delete).WithName("DeleteBenefit").WithSummary("Exclui um benefício");
    }

    private static async Task<Created<Response>> Create(Request request, AppDbContext db, CancellationToken ct)
    {
        var benefit = new Benefit { Headline = request.Headline.Trim(), Title = request.Title.Trim() };
        Apply(benefit, request);
        db.Benefits.Add(benefit);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/api/admin/benefits/{benefit.Id}", new Response(benefit.Id, benefit.Version));
    }

    private static async Task<Results<Ok<Response>, NotFound>> Update(
        Guid id, Request request, AppDbContext db, CancellationToken ct)
    {
        var benefit = await db.Benefits.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (benefit is null) return TypedResults.NotFound();

        if (request.Version is { } version) db.Entry(benefit).Property(b => b.Version).OriginalValue = version;
        Apply(benefit, request);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new Response(benefit.Id, benefit.Version));
    }

    private static async Task<Results<NoContent, NotFound>> Delete(Guid id, AppDbContext db, CancellationToken ct)
    {
        var benefit = await db.Benefits.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (benefit is null) return TypedResults.NotFound();
        db.Benefits.Remove(benefit);
        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }

    private static void Apply(Benefit benefit, Request request)
    {
        benefit.Headline = request.Headline.Trim();
        benefit.Title = request.Title.Trim();
        benefit.Description = request.Description;
        benefit.VenueId = request.VenueId;
        benefit.Highlight = request.Highlight;
        benefit.SortOrder = request.SortOrder;
        benefit.IsActive = request.IsActive;
    }
}
