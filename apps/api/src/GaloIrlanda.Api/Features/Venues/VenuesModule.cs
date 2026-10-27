using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Venues;

public static class VenuesModule
{
    public static void MapVenuesEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/venues").WithTags("Admin · Locais");
        ListVenues.Map(admin);
        CreateVenue.Map(admin);
        UpdateVenue.Map(admin);
    }
}
