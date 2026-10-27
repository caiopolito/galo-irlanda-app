using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Polls;

public static class PollsModule
{
    public static void MapPollsEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/polls").WithTags("Admin · Votações");
        ListPolls.Map(admin);
        CreatePoll.Map(admin);
        UpdatePoll.Map(admin);
        DeletePoll.Map(admin);
    }
}
