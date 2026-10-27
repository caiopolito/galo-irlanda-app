using System.Net;
using System.Net.Http.Json;
using GaloIrlanda.Api.Features.Health;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GaloIrlanda.Api.Tests.Features.Health;

public sealed class GetHealthTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Returns_ok()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetHealth.Response>(TestContext.Current.CancellationToken);
        Assert.Equal("ok", body?.Status);
    }
}
