using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Membership;

public static class MembershipModule
{
    public static IServiceCollection AddMembership(this IServiceCollection services) =>
        services.AddScoped<ProvisionMember>();

    public static void MapMembershipEndpoints(this EndpointGroups groups, IEndpointRouteBuilder app)
    {
        StartCheckout.Map(groups.Public.MapGroup("/membership").WithTags("Público"));
        // Fora de /api: o Stripe não manda o header anti-CSRF; a autenticidade vem da assinatura.
        StripeWebhook.Map(app);
    }
}
