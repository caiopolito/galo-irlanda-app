using GaloIrlanda.Api.Common.Http;
using GaloIrlanda.Api.Integrations.Cloudflare;

namespace GaloIrlanda.Api.Features.Publishing;

public static class PublishingModule
{
    public static IServiceCollection AddPublishing(this IServiceCollection services)
    {
        services.AddHttpClient<IDeployHook, CloudflareDeployHook>();
        services.AddScoped<SiteRebuilder>();
        return services;
    }

    public static void MapPublishingEndpoints(this EndpointGroups groups)
    {
        RebuildNow.Map(groups.Admin.MapGroup("/publishing").WithTags("Admin · Publicação"));
        CronTick.Map(groups.Internal);
    }
}
