using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Landing;

public static class LandingModule
{
    public static void MapLandingEndpoints(this EndpointGroups groups) =>
        GetLanding.Map(groups.Public.WithTags("Público"));
}
