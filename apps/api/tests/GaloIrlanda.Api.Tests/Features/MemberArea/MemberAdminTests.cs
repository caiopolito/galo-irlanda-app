using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Paging;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Benefits;
using GaloIrlanda.Api.Features.Documents;
using GaloIrlanda.Api.Features.MemberArea;
using GaloIrlanda.Api.Features.Members;
using GaloIrlanda.Api.Features.Photos;
using GaloIrlanda.Api.Features.Polls;
using GaloIrlanda.Api.Tests.Support;

namespace GaloIrlanda.Api.Tests.Features.MemberArea;

/// <summary>Admin da área do sócio: votações, benefícios, documentos, sócios e marcação em fotos.</summary>
public sealed class MemberAdminTests(ApiFactory api) : IAsyncLifetime
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => api.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Poll_options_lock_after_the_first_vote()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);
        var (member, _) = await api.CreateMemberClientAsync();
        var now = api.Clock.GetUtcNow();
        var created = await (await editor.PostAsJsonAsync("/api/admin/polls",
                new CreatePoll.Request("Churrasco onde?", null, "se chover, é no pub", true, now.AddHours(-1), now.AddDays(2), ["Quintal", "Parque"], true),
                Json.Options, Ct))
            .Content.ReadFromJsonAsync<CreatePoll.Response>(Json.Options, Ct);

        var poll = (await editor.GetFromJsonAsync<List<ListPolls.Item>>("/api/admin/polls", Json.Options, Ct))!.Single();
        await member.PostAsJsonAsync($"/api/socio/polls/{poll.Id}/vote", new VotePoll.Request(poll.Options[0].Id), Json.Options, Ct);

        var renameOptions = await editor.PutAsJsonAsync($"/api/admin/polls/{poll.Id}",
            new UpdatePoll.Request("Churrasco onde?", null, null, true, now.AddHours(-1), now.AddDays(2), ["Quintal", "Pub"], true, created!.Version),
            Json.Options, Ct);
        var extendDeadline = await editor.PutAsJsonAsync($"/api/admin/polls/{poll.Id}",
            new UpdatePoll.Request("Churrasco onde?", null, null, true, now.AddHours(-1), now.AddDays(5), ["Quintal", "Parque"], true, created.Version),
            Json.Options, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, renameOptions.StatusCode);
        Assert.Equal(HttpStatusCode.OK, extendDeadline.StatusCode);
        var after = (await editor.GetFromJsonAsync<List<ListPolls.Item>>("/api/admin/polls", Json.Options, Ct))!.Single();
        Assert.Equal(1, after.Participants);
        Assert.Equal(1, after.Options[0].Votes);
    }

    [Fact]
    public async Task Benefits_created_in_the_admin_show_up_for_members()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);
        var (member, _) = await api.CreateMemberClientAsync();

        await editor.PostAsJsonAsync("/api/admin/benefits",
            new SaveBenefit.Request("10%", "no The Pub", "Em dia de jogo.", null, true, 1, true, null), Json.Options, Ct);
        await editor.PostAsJsonAsync("/api/admin/benefits",
            new SaveBenefit.Request("Kit", "camisa", null, null, false, 2, false, null), Json.Options, Ct);

        var benefits = await member.GetFromJsonAsync<List<ListMemberBenefits.Item>>("/api/socio/benefits", Json.Options, Ct);
        Assert.Equal("10%", Assert.Single(benefits!).Headline);
    }

    [Fact]
    public async Task Uploaded_documents_reach_members_only_when_published()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);
        var (member, _) = await api.CreateMemberClientAsync();
        var file = new ByteArrayContent("%PDF-ata"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var form = new MultipartFormDataContent { { file, "file", "ata.pdf" }, { new StringContent("Ata de outubro"), "title" } };

        var upload = await editor.PostAsync("/api/admin/documents", form, Ct);
        var created = await upload.Content.ReadFromJsonAsync<UploadDocument.Response>(Json.Options, Ct);
        Assert.Empty((await member.GetFromJsonAsync<List<MemberDocuments.Item>>("/api/socio/documents", Json.Options, Ct))!);

        await editor.PutAsJsonAsync($"/api/admin/documents/{created!.Id}",
            new ManageDocuments.UpdateRequest("Ata de outubro", 1, true, created.Version), Json.Options, Ct);

        var listed = Assert.Single((await member.GetFromJsonAsync<List<MemberDocuments.Item>>("/api/socio/documents", Json.Options, Ct))!);
        Assert.Equal("ata-de-outubro.pdf", listed.FileName);
        Assert.Equal("%PDF-ata"u8.ToArray(), await member.GetByteArrayAsync($"/api/socio/documents/{listed.Id}/download", Ct));
    }

    [Fact]
    public async Task Members_list_shows_status_and_search_by_number()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);
        await api.CreateMemberClientAsync(name: "Maria em dia");
        await api.CreateMemberClientAsync(active: false, name: "João atrasado");

        var all = await editor.GetFromJsonAsync<PagedResponse<ListMembers.Item>>("/api/admin/members", Json.Options, Ct);
        var joao = all!.Items.Single(m => m.FullName == "João atrasado");
        var byNumber = await editor.GetFromJsonAsync<PagedResponse<ListMembers.Item>>(
            $"/api/admin/members?search={joao.MemberNumber}", Json.Options, Ct);

        Assert.Equal(2, all.Total);
        Assert.Equal("past_due", joao.Status);
        Assert.Equal("João atrasado", Assert.Single(byNumber!.Items).FullName);
    }

    [Fact]
    public async Task Only_members_who_opted_in_can_be_tagged()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);
        var (_, optedIn) = await api.CreateMemberClientAsync(name: "Aceita");
        var (_, optedOut) = await api.CreateMemberClientAsync(name: "Não aceita");
        await using (var db = api.CreateDbContext())
        {
            (await db.Members.FindAsync([optedOut], Ct))!.PhotoTagOptIn = false;
            await db.SaveChangesAsync(Ct);
        }
        var photo = await api.SeedAsync(new Photo { UploadStatus = PhotoUploadStatus.Ready, Width = 10, Height = 10 });

        var response = await editor.PutAsJsonAsync($"/api/admin/photos/{photo.Id}/tags",
            new TagPhotoMembers.Request([optedIn, optedOut]), Json.Options, Ct);

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync(Ct));
        var tagged = await response.Content.ReadFromJsonAsync<List<TagPhotoMembers.Tagged>>(Json.Options, Ct);
        Assert.Equal("Aceita", Assert.Single(tagged!).FullName);
    }
}
