using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Publishing;

/// <summary>Botão "Publicar agora" do admin: regenera o site sem esperar.</summary>
public static class RebuildNow
{
    public sealed record Response(bool Triggered);

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/rebuild", Handle)
            .WithName(nameof(RebuildNow))
            .WithSummary("Regenera o site público agora");

    private static async Task<Ok<Response>> Handle(SiteRebuilder rebuilder, CancellationToken ct) =>
        TypedResults.Ok(new Response(await rebuilder.TryTriggerAsync(force: true, ct)));
}
