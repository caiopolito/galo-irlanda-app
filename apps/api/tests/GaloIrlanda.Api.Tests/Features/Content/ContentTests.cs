using System.Net;
using System.Net.Http.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Cities;
using GaloIrlanda.Api.Features.History;
using GaloIrlanda.Api.Features.Milestones;
using GaloIrlanda.Api.Features.Settings;
using GaloIrlanda.Api.Features.Venues;
using GaloIrlanda.Api.Tests.Support;

namespace GaloIrlanda.Api.Tests.Features.Content;

/// <summary>Cidades, locais, marcos, configurações e histórico.</summary>
public sealed class ContentTests(ApiFactory api) : IAsyncLifetime
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await api.ResetAsync();
        await api.SeedAsync(new City { Code = "DUB", Name = "Dublin" });
        await api.SeedAsync(new SiteSettings());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Editing_a_city_is_recorded_in_the_history_with_the_author()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);
        var city = Assert.Single((await editor.GetFromJsonAsync<List<ListCities.Item>>("/api/admin/cities", Json.Options, Ct))!);

        var response = await editor.PutAsJsonAsync("/api/admin/cities/dub",
            new UpdateCity.Request("Dublin", "Sede da bagunça.", null, 1, true, city.Version), Json.Options, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var admin = await api.CreateClientAsAsync(Roles.Admin);
        var history = await admin.GetFromJsonAsync<PagedResponse<ListChanges.Item>>(
            "/api/admin/history?entityType=city&entityId=DUB", Json.Options, Ct);
        var change = history!.Items.First(i => i.Action == "update");
        Assert.Equal("Teste editor", change.ActorName);
        Assert.Contains("Sede da bagunça.", change.Changes);
    }

    [Fact]
    public async Task Venue_links_must_be_https_and_city_must_exist()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);

        var badUrl = await editor.PostAsJsonAsync("/api/admin/venues",
            new CreateVenue.Request("DUB", "Pub", null, null, "http://maps", null, true, true, null), Json.Options, Ct);
        var badCity = await editor.PostAsJsonAsync("/api/admin/venues",
            new CreateVenue.Request("XXX", "Pub", null, null, null, null, true, true, null), Json.Options, Ct);
        var ok = await editor.PostAsJsonAsync("/api/admin/venues",
            new CreateVenue.Request("dub", "The Pub", "Temple Bar", null, "https://maps.app/x", null, true, true, null), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, badUrl.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, badCity.StatusCode);
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        var venues = await editor.GetFromJsonAsync<List<ListVenues.Item>>("/api/admin/venues?cityCode=DUB", Json.Options, Ct);
        Assert.Equal("The Pub", Assert.Single(venues!).Name);
    }

    [Fact]
    public async Task Milestones_are_ordered_by_year()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);
        await editor.PostAsJsonAsync("/api/admin/milestones", new CreateMilestone.Request(2013, "Libertadores.", null, 0, true), Json.Options, Ct);
        await editor.PostAsJsonAsync("/api/admin/milestones", new CreateMilestone.Request(1908, "Fundação.", null, 0, true), Json.Options, Ct);

        var list = await editor.GetFromJsonAsync<List<ListMilestones.Item>>("/api/admin/milestones", Json.Options, Ct);

        Assert.Equal([(short)1908, (short)2013], list!.Select(m => m.Year));
    }

    [Fact]
    public async Task Settings_reject_non_https_links_and_save_valid_ones()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);
        var current = await editor.GetFromJsonAsync<GetSiteSettings.Response>("/api/admin/settings", Json.Options, Ct);

        var bad = await editor.PutAsJsonAsync("/api/admin/settings",
            new UpdateSiteSettings.Request(null, 1500, "http://wa.me/x", null, null, current!.Version), Json.Options, Ct);
        var good = await editor.PutAsJsonAsync("/api/admin/settings",
            new UpdateSiteSettings.Request(null, 1500, "https://chat.whatsapp.com/x", null, "contato@teste.local", current.Version), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        var saved = await editor.GetFromJsonAsync<GetSiteSettings.Response>("/api/admin/settings", Json.Options, Ct);
        Assert.Equal(1500, saved!.MembershipFeeCents);
    }
}
