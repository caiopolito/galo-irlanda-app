using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Http;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Photos;
using GaloIrlanda.Api.Tests.Support;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Tests.Features.Photos;

public sealed class PhotosTests(ApiFactory api) : IAsyncLifetime
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => api.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static MultipartFormDataContent Form(byte[] bytes, string contentType)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { file, "file", "foto.jpg" } };
    }

    private async Task<UploadPhoto.Response> Upload(HttpClient client, int width = 3000, int height = 2000)
    {
        var response = await client.PostAsync("/api/admin/photos", Form(TestImages.Jpeg(width, height), "image/jpeg"), Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<UploadPhoto.Response>(Json.Options, Ct))!;
    }

    private static UpdatePhoto.Request Publish(uint version) => new(
        AltText: "Torcida no pub", Title: "Gol", Caption: "Ninguém sentado.", TakenOn: null, EventId: null,
        Visibility: PhotoVisibility.Public, ConsentBasis: ConsentBasis.ConsentRecorded, ConsentNote: "Ok no grupo",
        WallFeatured: true, WallSortOrder: 1, Version: version);

    [Fact]
    public async Task Upload_generates_three_webp_variants_without_upscaling()
    {
        var client = await api.CreateClientAsAsync(Roles.Editor);

        var photo = await Upload(client, width: 1200, height: 800);

        Assert.Equal((1200, 800), (photo.Width, photo.Height));
        Assert.Equal($"https://fotos.teste/photos/{photo.Id}/", photo.BaseUrl);
        Assert.Equal((480, 320), TestImages.SizeOf(api.Storage.Objects[PhotoProcessor.Key(photo.Id, 480)]));
        Assert.Equal((1024, 683), TestImages.SizeOf(api.Storage.Objects[PhotoProcessor.Key(photo.Id, 1024)]));
        Assert.Equal((1200, 800), TestImages.SizeOf(api.Storage.Objects[PhotoProcessor.Key(photo.Id, 2048)]));

        await using var db = api.CreateDbContext();
        var saved = await db.Photos.SingleAsync(Ct);
        Assert.Equal(PhotoVisibility.Private, saved.Visibility);
        Assert.Equal(PhotoUploadStatus.Ready, saved.UploadStatus);
    }

    [Fact]
    public async Task Rejects_files_that_are_not_images()
    {
        var client = await api.CreateClientAsAsync(Roles.Editor);

        var wrongType = await client.PostAsync("/api/admin/photos", Form([1, 2, 3], "application/pdf"), Ct);
        var corrupt = await client.PostAsync("/api/admin/photos", Form([1, 2, 3], "image/jpeg"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, wrongType.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, corrupt.StatusCode);
        Assert.Empty(api.Storage.Objects);
    }

    [Fact]
    public async Task Public_photo_requires_consent_and_alt_text()
    {
        var client = await api.CreateClientAsAsync(Roles.Editor);
        var photo = await Upload(client);

        var response = await client.PutAsJsonAsync($"/api/admin/photos/{photo.Id}",
            Publish(photo.Version) with { AltText = null, ConsentBasis = ConsentBasis.Pending }, Json.Options, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct);
        Assert.Contains("altText", problem!.Errors.Keys);
        Assert.Contains("consentBasis", problem.Errors.Keys);
    }

    [Fact]
    public async Task Publishing_records_who_confirmed_consent_and_rebuilds_the_site()
    {
        var (client, editorId) = await api.CreateClientWithUserAsync(Roles.Editor);
        var photo = await Upload(client);

        var response = await client.PutAsJsonAsync($"/api/admin/photos/{photo.Id}", Publish(photo.Version), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = api.CreateDbContext();
        var saved = await db.Photos.SingleAsync(Ct);
        Assert.Equal(editorId, saved.ConsentConfirmedBy);
        Assert.NotNull(saved.ConsentConfirmedAt);
        Assert.Equal(1, api.DeployHook.Calls);
    }

    [Fact]
    public async Task Deleted_photo_leaves_the_site_and_is_purged_by_the_cron()
    {
        var client = await api.CreateClientAsAsync(Roles.Editor);
        var photo = await Upload(client);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/admin/photos/{photo.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/admin/photos/{photo.Id}", Ct)).StatusCode);
        Assert.Equal(3, api.Storage.Objects.Count);

        var cron = api.CreateClient();
        cron.DefaultRequestHeaders.Add(InternalSecretFilter.HeaderName, "segredo-de-teste");
        Assert.Equal(HttpStatusCode.OK, (await cron.PostAsync("/internal/cron/tick", null, Ct)).StatusCode);

        Assert.Empty(api.Storage.Objects);
        await using var db = api.CreateDbContext();
        Assert.False(await db.Photos.AnyAsync(Ct));
    }

    [Fact]
    public async Task Cron_requires_the_internal_secret()
    {
        var response = await api.CreateClient().PostAsync("/internal/cron/tick", null, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
