using System.Net;
using System.Net.Http.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Http;
using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Posts;
using GaloIrlanda.Api.Tests.Support;

namespace GaloIrlanda.Api.Tests.Features.Posts;

public sealed class PostsTests(ApiFactory api) : IAsyncLifetime
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await api.ResetAsync();
        await api.SeedAsync(new PostCategory { Slug = "encontros", Name = "Encontros" });
        api.Clock.Advance(TimeSpan.FromMinutes(5));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<CreatePost.Response> CreateDraft(HttpClient client, string title = "Achamos um pub com som")
    {
        var response = await client.PostAsJsonAsync("/api/admin/posts",
            new CreatePost.Request(title, null, "Texto", null, null), Json.Options, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatePost.Response>(Json.Options, Ct))!;
    }

    private async Task<List<string>> PublicTitles()
    {
        var page = await api.CreateClient()
            .GetFromJsonAsync<PagedResponse<ListPublishedPosts.Item>>("/api/public/posts", Json.Options, Ct);
        return page!.Items.Select(i => i.Title).ToList();
    }

    [Fact]
    public async Task Draft_is_hidden_until_published()
    {
        var client = await api.CreateClientAsAsync(Roles.Editor);
        var post = await CreateDraft(client);
        Assert.Equal("achamos-um-pub-com-som", post.Slug);
        Assert.Empty(await PublicTitles());

        await client.PostAsJsonAsync($"/api/admin/posts/{post.Id}/publish", new PublishPost.Request(null), Json.Options, Ct);

        Assert.Equal(["Achamos um pub com som"], await PublicTitles());
        var detail = await api.CreateClient().GetFromJsonAsync<GetPublishedPost.Response>($"/api/public/posts/{post.Slug}", Json.Options, Ct);
        Assert.Equal("Texto", detail!.BodyMd);
    }

    [Fact]
    public async Task Scheduled_post_appears_when_its_time_comes_and_the_cron_rebuilds()
    {
        var client = await api.CreateClientAsAsync(Roles.Editor);
        var post = await CreateDraft(client);
        await client.PostAsJsonAsync($"/api/admin/posts/{post.Id}/publish",
            new PublishPost.Request(api.Clock.GetUtcNow().AddHours(2)), Json.Options, Ct);
        var callsAfterScheduling = api.DeployHook.Calls;

        Assert.Empty(await PublicTitles());

        api.Clock.Advance(TimeSpan.FromHours(3));
        var cron = api.CreateClient();
        cron.DefaultRequestHeaders.Add(InternalSecretFilter.HeaderName, "segredo-de-teste");
        await cron.PostAsync("/internal/cron/tick", null, Ct);

        Assert.Single(await PublicTitles());
        Assert.Equal(callsAfterScheduling + 1, api.DeployHook.Calls);
    }

    [Fact]
    public async Task Archived_post_leaves_the_site()
    {
        var client = await api.CreateClientAsAsync(Roles.Editor);
        var post = await CreateDraft(client);
        await client.PostAsJsonAsync($"/api/admin/posts/{post.Id}/publish", new PublishPost.Request(null), Json.Options, Ct);

        await client.PostAsync($"/api/admin/posts/{post.Id}/archive", null, Ct);

        Assert.Empty(await PublicTitles());
    }

    [Fact]
    public async Task Unknown_category_is_rejected()
    {
        var client = await api.CreateClientAsAsync(Roles.Editor);

        var response = await client.PostAsJsonAsync("/api/admin/posts",
            new CreatePost.Request("Título", null, "", 999, null), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
