namespace GaloIrlanda.Api.Common.Http;

/// <summary>
/// Mutações exigem o header <c>X-Galo-Csrf</c>: ele força o preflight do CORS, que só libera as
/// origens do site. Junto com o cookie <c>SameSite=Lax</c>, cobre CSRF (ADR 0006).
/// </summary>
public sealed class CsrfHeaderFilter : IEndpointFilter
{
    public const string HeaderName = "X-Galo-Csrf";

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        var isMutation = !(HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) ||
                           HttpMethods.IsOptions(request.Method));

        if (isMutation && !request.Headers.ContainsKey(HeaderName))
        {
            return ValueTask.FromResult<object?>(TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: $"Header {HeaderName} ausente."));
        }

        return next(context);
    }
}
