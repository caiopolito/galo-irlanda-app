using System.Net.Http.Json;
using GaloIrlanda.Api.Features.Landing;
using GaloIrlanda.Api.Features.Seed;
using GaloIrlanda.Api.Tests.Support;

namespace GaloIrlanda.Api.Tests.Features.Landing;

public sealed class LandingTests(ApiFactory api) : IAsyncLifetime
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => api.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Layout_seed_fills_every_section_of_the_landing()
    {
        var summary = await SeedLayoutContent.RunAsync(api.Services, TestImages.LayoutPhotosPath(), Ct);
        Assert.StartsWith("Cadastrados: 6 fotos", summary);

        var landing = await api.CreateClient().GetFromJsonAsync<GetLanding.Response>("/api/public/landing", Json.Options, Ct);

        Assert.NotNull(landing!.Hero);
        Assert.Equal(["Galo × [Adversário]", "Pelada da Massa", "Galo × [Adversário]", "Churrasco mineiro"],
            landing.Events.Select(e => e.Title));
        Assert.Equal("[Pub parceiro], [bairro]", landing.NextMatch!.Place);
        Assert.NotNull(landing.NextMatch.Photo);
        Assert.Equal(38, landing.NextMatch.Confirmed);
        Assert.Equal(["DUB", "ORK", "GWY"], landing.Cities.Select(c => c.Code));
        Assert.Equal(3, landing.News.Count);
        Assert.Equal(["Churrasco da Massa.", "Pub dividido.", "Pint na mesa,", "Faixa aberta."],
            landing.WallPhotos.Select(p => p.Title));
        Assert.Equal(5, landing.Milestones.Count);
        Assert.Equal(18, api.Storage.Objects.Count); // 6 fotos × 3 larguras
    }

    [Fact]
    public async Task Layout_seed_runs_only_once()
    {
        await SeedLayoutContent.RunAsync(api.Services, TestImages.LayoutPhotosPath(), Ct);

        var second = await SeedLayoutContent.RunAsync(api.Services, TestImages.LayoutPhotosPath(), Ct);

        Assert.StartsWith("Já existe conteúdo", second);
    }
}
