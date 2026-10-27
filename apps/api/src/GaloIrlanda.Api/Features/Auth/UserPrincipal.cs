using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GaloIrlanda.Api.Features.Auth;

public static class UserPrincipal
{
    public static ClaimsPrincipal Create(Guid userId, string displayName, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, displayName),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
