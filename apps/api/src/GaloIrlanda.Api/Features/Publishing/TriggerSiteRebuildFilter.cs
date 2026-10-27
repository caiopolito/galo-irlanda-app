namespace GaloIrlanda.Api.Features.Publishing;

/// <summary>Depois de cada mutação no admin, tenta regenerar o site se houver pedido pendente.</summary>
public sealed class TriggerSiteRebuildFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
        {
            var rebuilder = context.HttpContext.RequestServices.GetRequiredService<SiteRebuilder>();
            await rebuilder.TryTriggerAsync(force: false, context.HttpContext.RequestAborted);
        }

        return result;
    }
}
