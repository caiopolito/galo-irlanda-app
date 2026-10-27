using Microsoft.AspNetCore.Http.HttpResults;

namespace GaloIrlanda.Api.Features.Health;

public static class GetHealth
{
    public sealed record Response(string Status);

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/health", Handle)
            .WithName(nameof(GetHealth))
            .WithTags("Health");

    private static Ok<Response> Handle() => TypedResults.Ok(new Response("ok"));
}
