using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Http;

namespace GaloIrlanda.Api.Features.Users;

public static class UsersModule
{
    public static void MapUsersEndpoints(this EndpointGroups groups)
    {
        var admin = groups.Admin.MapGroup("/users").WithTags("Admin · Usuários").RequireAuthorization(Policies.Admin);
        ListUsers.Map(admin);
        InviteUser.Map(admin);
        ChangeUserRoles.Map(admin);
        SetUserStatus.Map(admin);
    }
}
