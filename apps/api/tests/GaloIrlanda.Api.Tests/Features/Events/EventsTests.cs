using System.Net;
using System.Net.Http.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Http;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Events;
using GaloIrlanda.Api.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Tests.Features.Events;

public sealed class EventsTests(ApiFactory api) : IAsyncLifetime
{
    private static readonly DateTimeOffset KickoffInBh = new(2026, 11, 6, 21, 30, 0, TimeSpan.FromHours(-3));
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await api.ResetAsync();
        await api.SeedAsync(new City { Code = "DUB", Name = "Dublin" });
        // A cidade é conteúdo público: o seed deixa um pedido de regeração que não é do teste.
        await using (var db = api.CreateDbContext())
        {
            await db.SiteRebuildRequests.ExecuteDeleteAsync(Ct);
        }
        // Espaça os testes para a espera mínima entre regerações não interferir.
        api.Clock.Advance(TimeSpan.FromMinutes(5));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static CreateEvent.Request NewMatch(bool publish = true, string? opponent = "Adversário") => new(
        Kind: EventKind.Match,
        Title: "Galo × Adversário",
        StartsAt: KickoffInBh,
        CityCode: "DUB",
        VenueId: null,
        VenueNote: "Pub parceiro",
        Opponent: opponent,
        Competition: "Brasileirão",
        HomeTrip: null,
        DescriptionMd: null,
        Status: EventStatus.Confirmed,
        Capacity: null,
        SpotsTaken: 0,
        Publish: publish);

    private async Task<CreateEvent.Response> CreateAsync(CreateEvent.Request request)
    {
        var response = await (await api.CreateClientAsAsync(Roles.Editor)).PostAsJsonAsync("/api/admin/events", request, Json.Options, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreateEvent.Response>(Json.Options, Ct))!;
    }

    [Fact]
    public async Task Editor_creates_published_event_in_utc_with_history_and_site_rebuild()
    {
        var created = await CreateAsync(NewMatch());

        await using var db = api.CreateDbContext();
        var saved = await db.Events.SingleAsync(e => e.Id == created.Id, Ct);
        Assert.Equal(new DateTimeOffset(2026, 11, 7, 0, 30, 0, TimeSpan.Zero), saved.StartsAt);
        Assert.Equal("2026-11-07-galo-adversario", saved.Slug);
        Assert.NotNull(saved.CreatedBy);
        Assert.True(await db.AuditEntries.AnyAsync(a => a.EntityType == "event" && a.Action == "create", Ct));
        Assert.Equal(1, api.DeployHook.Calls);
        Assert.False(await db.SiteRebuildRequests.AnyAsync(r => r.TriggeredAt == null, Ct));
    }

    [Fact]
    public async Task Draft_event_does_not_rebuild_the_site()
    {
        await CreateAsync(NewMatch(publish: false));

        Assert.Equal(0, api.DeployHook.Calls);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(Roles.Socio, HttpStatusCode.Forbidden)]
    public async Task Only_editors_can_create(string? role, HttpStatusCode expected)
    {
        var response = await (await api.CreateClientAsAsync(role)).PostAsJsonAsync("/api/admin/events", NewMatch(), Json.Options, Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Match_without_opponent_is_rejected()
    {
        var response = await (await api.CreateClientAsAsync(Roles.Editor))
            .PostAsJsonAsync("/api/admin/events", NewMatch(opponent: null), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Contains("opponent", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Mutation_without_csrf_header_is_forbidden()
    {
        var client = await api.CreateClientAsAsync(Roles.Editor);
        client.DefaultRequestHeaders.Remove(CsrfHeaderFilter.HeaderName);

        var response = await client.PostAsJsonAsync("/api/admin/events", NewMatch(), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_with_stale_version_returns_conflict()
    {
        var created = await CreateAsync(NewMatch());
        var client = await api.CreateClientAsAsync(Roles.Editor);
        UpdateEvent.Request Edit(string title, uint version) => new(
            EventKind.Match, title, KickoffInBh, "DUB", null, "Pub parceiro", "Adversário", null, null, null,
            EventStatus.Confirmed, null, 0, version);

        var first = await client.PutAsJsonAsync($"/api/admin/events/{created.Id}", Edit("Primeira edição", created.Version), Json.Options, Ct);
        var second = await client.PutAsJsonAsync($"/api/admin/events/{created.Id}", Edit("Edição atrasada", created.Version), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        await using var db = api.CreateDbContext();
        Assert.Equal("Primeira edição", (await db.Events.SingleAsync(e => e.Id == created.Id, Ct)).Title);
    }

    [Fact]
    public async Task Public_board_lists_only_published_and_not_cancelled_events_in_order()
    {
        await CreateAsync(NewMatch() with { Title = "Depois", StartsAt = KickoffInBh.AddDays(7) });
        await CreateAsync(NewMatch() with { Title = "Antes" });
        await CreateAsync(NewMatch(publish: false) with { Title = "Rascunho" });
        await CreateAsync(NewMatch() with { Title = "Cancelado", Status = EventStatus.Cancelled });

        var items = await (await api.CreateClientAsAsync(role: null))
            .GetFromJsonAsync<List<ListUpcomingEvents.Item>>("/api/public/events", Json.Options, Ct);

        Assert.NotNull(items);
        Assert.Equal(["Antes", "Depois"], items.Select(i => i.Title));
        Assert.Equal("Dublin", items[0].CityName);
        Assert.Equal("Pub parceiro", items[0].Place);
    }

    [Fact]
    public async Task Unpublishing_rebuilds_the_site()
    {
        var created = await CreateAsync(NewMatch());
        api.Clock.Advance(TimeSpan.FromMinutes(3));

        var response = await (await api.CreateClientAsAsync(Roles.Editor)).PostAsync($"/api/admin/events/{created.Id}/unpublish", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(2, api.DeployHook.Calls);
    }

    [Fact]
    public async Task Only_admins_can_delete()
    {
        var created = await CreateAsync(NewMatch(publish: false));

        var asEditor = await (await api.CreateClientAsAsync(Roles.Editor)).DeleteAsync($"/api/admin/events/{created.Id}", Ct);
        var asAdmin = await (await api.CreateClientAsAsync(Roles.Admin)).DeleteAsync($"/api/admin/events/{created.Id}", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, asEditor.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, asAdmin.StatusCode);
    }
}
