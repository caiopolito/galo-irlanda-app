using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GaloIrlanda.Api.Common.Http;

/// <summary>Traduz conflitos do banco em 409 (ProblemDetails) em vez de 500.</summary>
public sealed class DbExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var title = exception switch
        {
            DbUpdateConcurrencyException =>
                "Este item foi alterado por outra pessoa. Recarregue a página e tente de novo.",
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                "Já existe um item com esses dados.",
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                "Este item está em uso por outro registro.",
            _ => null,
        };
        if (title is null) return false;

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = StatusCodes.Status409Conflict, Title = title },
        });
    }
}
