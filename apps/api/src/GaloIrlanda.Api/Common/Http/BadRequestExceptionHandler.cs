using Microsoft.AspNetCore.Diagnostics;

namespace GaloIrlanda.Api.Common.Http;

/// <summary>
/// Corpo ou parâmetro que não dá para ler (JSON malformado, GUID inválido) vira 400, não 500.
/// Em desenvolvimento o ASP.NET lança essas exceções em vez de responder 400 sozinho.
/// </summary>
public sealed class BadRequestExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not BadHttpRequestException badRequest) return false;

        httpContext.Response.StatusCode = badRequest.StatusCode;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = { Status = badRequest.StatusCode, Title = "Requisição inválida." },
        });
    }
}
