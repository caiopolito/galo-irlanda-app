using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.MemberArea;

public static class MemberAreaModule
{
    public static IServiceCollection AddMemberArea(this IServiceCollection services) =>
        services.AddSingleton<MemberCard>();

    public static void MapMemberAreaEndpoints(this EndpointGroups groups)
    {
        GetMemberDashboard.Map(groups.Member);
        UpdateMemberProfile.Map(groups.Member);
        GetMemberCard.Map(groups.Member);
        ListMemberEvents.Map(groups.Member);
        SetRsvp.Map(groups.Member);
        GetBilling.Map(groups.Member);
        ListMemberPolls.Map(groups.Member);
        VotePoll.Map(groups.Member);
        ListMemberBenefits.Map(groups.Member);
        MyPhotos.Map(groups.Member);
        MemberDocuments.Map(groups.Member);

        VerifyMemberCard.Map(groups.Public.WithTags("Público"));
    }
}
