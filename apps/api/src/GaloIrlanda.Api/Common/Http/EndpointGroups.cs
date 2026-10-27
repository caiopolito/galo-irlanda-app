using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Features.Publishing;

namespace GaloIrlanda.Api.Common.Http;

/// <summary>Grupos de rota: quem pode chamar um slice é decidido pelo grupo em que o módulo o registra.</summary>
public sealed record EndpointGroups(RouteGroupBuilder Admin, RouteGroupBuilder Public, RouteGroupBuilder Internal);

public static class EndpointGroupsExtensions
{
    public static EndpointGroups MapEndpointGroups(this WebApplication app)
    {
        var api = app.MapGroup("/api").AddEndpointFilter<CsrfHeaderFilter>();

        var admin = api.MapGroup("/admin")
            .RequireAuthorization(Policies.Editor)
            .AddEndpointFilter<TriggerSiteRebuildFilter>();

        var @public = api.MapGroup("/public").AllowAnonymous();

        var @internal = app.MapGroup("/internal")
            .AddEndpointFilter<InternalSecretFilter>()
            .ExcludeFromDescription();

        return new EndpointGroups(admin, @public, @internal);
    }
}
