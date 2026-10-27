using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GaloIrlanda.Api.Tests.Support;

/// <summary>
/// Autentica pelo header <c>X-Test-User: {id}:{papel}</c>. Sem o header, usa o cookie de sessão real
/// (testes do login) ou fica anônimo.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-User";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var value))
        {
            return await Context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        var (userId, role) = value.ToString().Split(':') switch
        {
            [var id, var r] => (id, r),
            _ => throw new InvalidOperationException($"{HeaderName} deve ser '{{id}}:{{papel}}'."),
        };

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, role)], SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }
}
