using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Settings;

public static class SettingsModule
{
    public static void MapSettingsEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/settings").WithTags("Admin · Configurações");
        GetSiteSettings.Map(admin);
        UpdateSiteSettings.Map(admin);
    }
}
