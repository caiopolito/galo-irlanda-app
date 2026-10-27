using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Benefits;

public static class BenefitsModule
{
    public static void MapBenefitsEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/benefits").WithTags("Admin · Benefícios");
        ListBenefits.Map(admin);
        SaveBenefit.Map(admin);
    }
}
