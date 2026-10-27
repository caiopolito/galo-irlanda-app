using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Stripe;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GaloIrlanda.Api.Features.Membership;

/// <summary>
/// "Seja sócio": valida os dados e devolve o endereço do checkout do Stripe. A pessoa só vira sócia
/// quando o pagamento é confirmado, no webhook (<see cref="StripeWebhook"/>). Os dados viajam nos
/// metadados do checkout; nada é gravado antes de pagar.
/// </summary>
public static class StartCheckout
{
    public sealed record Request(string FullName, string Email, string CityCode, string? Phone, string? ReferralCode);

    public sealed record Response(string CheckoutUrl);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
            RuleFor(x => x.CityCode).NotEmpty().Length(3);
            RuleFor(x => x.Phone).MaximumLength(30);
            RuleFor(x => x.ReferralCode).MaximumLength(12);
        }
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/checkout", Handle)
            .WithName(nameof(StartCheckout))
            .WithSummary("Começa a adesão: devolve o link de pagamento da mensalidade")
            .WithValidation<Request>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    private static async Task<Results<Ok<Response>, ValidationProblem, ProblemHttpResult>> Handle(
        Request request, AppDbContext db, IStripeGateway stripe, IOptions<StripeOptions> options,
        IConfiguration configuration, CancellationToken ct)
    {
        if (!options.Value.IsConfigured)
            return TypedResults.Problem(statusCode: 503, title: "O pagamento ainda não está configurado. Fale com a diretoria.");

        var email = request.Email.Trim();
        var cityCode = request.CityCode.ToUpperInvariant();
        if (!await db.Cities.AnyAsync(c => c.Code == cityCode && c.IsActive, ct))
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["cityCode"] = ["Cidade não encontrada."] });

        var alreadyMember = await db.Subscriptions.AnyAsync(s =>
            Subscription.ActiveStatuses.Contains(s.Status) &&
            db.Users.Any(u => u.Id == s.UserId && u.Email == email), ct);
        if (alreadyMember)
            return TypedResults.Problem(statusCode: 409, title: "Você já é sócio. Entre com o seu e-mail em /entrar.");

        var site = configuration["Site:PublicUrl"];
        var url = await stripe.CreateCheckoutUrlAsync(new CheckoutRequest(
            email,
            SuccessUrl: $"{site}/seja-socio/obrigado",
            CancelUrl: $"{site}/seja-socio?cancelado=1",
            Metadata: new Dictionary<string, string>
            {
                [MembershipMetadata.FullName] = request.FullName.Trim(),
                [MembershipMetadata.CityCode] = cityCode,
                [MembershipMetadata.Phone] = request.Phone?.Trim() ?? "",
                [MembershipMetadata.ReferralCode] = request.ReferralCode?.Trim().ToUpperInvariant() ?? "",
            }), ct);

        return TypedResults.Ok(new Response(url));
    }
}

/// <summary>Chaves dos metadados do checkout, lidas de volta no webhook.</summary>
public static class MembershipMetadata
{
    public const string FullName = "full_name";
    public const string CityCode = "city_code";
    public const string Phone = "phone";
    public const string ReferralCode = "referral_code";
}
