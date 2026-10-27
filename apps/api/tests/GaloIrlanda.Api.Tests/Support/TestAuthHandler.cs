using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GaloIrlanda.Api.Tests.Support;

/// <summary>Autentica pelo header <c>X-Test-User: {id}:{papel}</c>. Sem o header, a requisição é anônima.</summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string HeaderName = "X-Test-User";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var value))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var (userId, role) = value.ToString().Split(':') switch
        {
            [var id, var r] => (id, r),
            _ => throw new InvalidOperationException($"{HeaderName} deve ser '{{id}}:{{papel}}'."),
        };

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId), new Claim(ClaimTypes.Role, role)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
