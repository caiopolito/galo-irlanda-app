using System.Security.Claims;

namespace GaloIrlanda.Api.Common.Auth;

/// <summary>Usuário da requisição atual; <c>null</c> fora de uma requisição autenticada.</summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
{
    public Guid? Id =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;
}
