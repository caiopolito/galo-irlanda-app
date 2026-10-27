using System.Net;
using System.Net.Http.Json;
using GaloIrlanda.Api.Features.Health;
using GaloIrlanda.Api.Tests.Support;

namespace GaloIrlanda.Api.Tests.Features.Health;

public sealed class GetHealthTests(ApiFactory api)
{
    [Fact]
    public async Task Returns_ok()
    {
        var client = api.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetHealth.Response>(TestContext.Current.CancellationToken);
        Assert.Equal("ok", body?.Status);
    }
}
