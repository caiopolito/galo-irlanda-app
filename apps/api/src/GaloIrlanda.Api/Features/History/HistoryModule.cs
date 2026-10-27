using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.History;

public static class HistoryModule
{
    public static void MapHistoryEndpoints(this EndpointGroups groups) =>
        ListChanges.Map(groups.Admin.MapGroup("/history").WithTags("Admin · Histórico").RequireAuthorization(Policies.Admin));
}
