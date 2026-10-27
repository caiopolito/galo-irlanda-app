using System.Security.Cryptography;
using System.Text;

namespace GaloIrlanda.Api.Common.Http;

/// <summary>Protege <c>/internal/*</c> (chamado pelo cron) com um segredo compartilhado.</summary>
public sealed class InternalSecretFilter(IConfiguration configuration) : IEndpointFilter
{
    public const string HeaderName = "X-Internal-Secret";

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var expected = configuration["Internal:Secret"];
        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expected) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected)))
        {
            return ValueTask.FromResult<object?>(TypedResults.Unauthorized());
        }

        return next(context);
    }
}
