using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Cities;

public static class CitiesModule
{
    public static void MapCitiesEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/cities").WithTags("Admin · Cidades");
        ListCities.Map(admin);
        CreateCity.Map(admin);
        UpdateCity.Map(admin);
    }
}
