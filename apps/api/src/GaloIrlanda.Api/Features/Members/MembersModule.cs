using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Members;

public static class MembersModule
{
    public static void MapMembersEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/members").WithTags("Admin · Sócios");
        ListMembers.Map(admin);
        ReissueMemberCard.Map(admin);
    }
}
