using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Milestones;

public static class MilestonesModule
{
    public static void MapMilestonesEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/milestones").WithTags("Admin · Marcos");
        ListMilestones.Map(admin);
        CreateMilestone.Map(admin);
        UpdateMilestone.Map(admin);
        DeleteMilestone.Map(admin);
    }
}
