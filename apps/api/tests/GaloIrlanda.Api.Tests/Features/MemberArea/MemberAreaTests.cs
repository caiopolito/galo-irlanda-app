using System.Net;
using System.Net.Http.Json;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Landing;
using GaloIrlanda.Api.Features.MemberArea;
using GaloIrlanda.Api.Integrations.Stripe;
using GaloIrlanda.Api.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Tests.Features.MemberArea;

public sealed class MemberAreaTests(ApiFactory api) : IAsyncLifetime
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync() => api.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<Event> SeedEventAsync(DateTimeOffset startsAt, int? capacity = null, string title = "Galo × Adversário")
    {
        return await api.SeedAsync(new Event
        {
            Slug = $"e-{Guid.NewGuid():N}", Kind = EventKind.Match, Title = title, Opponent = "Adversário",
            StartsAt = startsAt, CityCode = "DUB", VenueNote = "Pub", Status = EventStatus.Confirmed,
            IsPublished = true, Capacity = capacity, SpotsTaken = 10,
        });
    }

    private Task<HttpResponseMessage> Rsvp(HttpClient client, Guid eventId, RsvpAnswer answer) =>
        client.PutAsJsonAsync($"/api/socio/events/{eventId}/rsvp", new SetRsvp.Request(answer), Json.Options, Ct);

    [Fact]
    public async Task Dashboard_shows_the_member_and_their_status()
    {
        var (member, _) = await api.CreateMemberClientAsync();

        var me = await member.GetFromJsonAsync<GetMemberDashboard.Response>("/api/socio/me", Json.Options, Ct);

        Assert.Equal("Maria da Silva", me!.FullName);
        Assert.True(me.MemberNumber > 0);
        Assert.True(me.Membership.IsActive);
        Assert.Equal(0, me.Attended);
    }

    [Fact]
    public async Task Member_area_is_only_for_members()
    {
        var editor = await api.CreateClientAsAsync(Roles.Editor);

        Assert.Equal(HttpStatusCode.Forbidden, (await editor.GetAsync("/api/socio/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Rsvp_counts_on_the_public_board_and_respects_capacity()
    {
        var (member, _) = await api.CreateMemberClientAsync();
        var (other, _) = await api.CreateMemberClientAsync(name: "Ana");
        var ev = await SeedEventAsync(api.Clock.GetUtcNow().AddDays(3), capacity: 11);

        Assert.Equal(HttpStatusCode.NoContent, (await Rsvp(member, ev.Id, RsvpAnswer.Going)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Rsvp(other, ev.Id, RsvpAnswer.Going)).StatusCode); // 10 + 1 = lotado
        Assert.Equal(HttpStatusCode.NoContent, (await Rsvp(other, ev.Id, RsvpAnswer.Maybe)).StatusCode);

        var mine = await member.GetFromJsonAsync<ListMemberEvents.Response>("/api/socio/events", Json.Options, Ct);
        Assert.Equal(RsvpAnswer.Going, mine!.Next!.MyAnswer);
        Assert.Equal(11, mine.Next.Going);
        var landing = await api.CreateClient().GetFromJsonAsync<GetLanding.Response>("/api/public/landing", Json.Options, Ct);
        Assert.Equal(11, landing!.NextMatch!.Confirmed);
    }

    [Fact]
    public async Task Past_events_no_longer_take_answers()
    {
        var (member, _) = await api.CreateMemberClientAsync();
        var ev = await SeedEventAsync(api.Clock.GetUtcNow().AddHours(-5));

        Assert.Equal(HttpStatusCode.BadRequest, (await Rsvp(member, ev.Id, RsvpAnswer.Going)).StatusCode);
    }

    [Fact]
    public async Task Card_qr_is_verifiable_and_reissuing_invalidates_old_codes()
    {
        var (member, userId) = await api.CreateMemberClientAsync();
        var card = await member.GetFromJsonAsync<GetMemberCard.Response>("/api/socio/card", Json.Options, Ct);
        Assert.Contains("<svg", card!.QrSvg);
        var token = card.VerificationUrl.Split("/carteirinha/")[1];

        var check = await api.CreateClient().GetFromJsonAsync<VerifyMemberCard.Response>($"/api/public/card/{token}", Json.Options, Ct);
        Assert.True(check!.Valid);
        Assert.True(check.IsActive);
        Assert.Equal("Maria da Silva", check.FullName);

        await using (var db = api.CreateDbContext())
        {
            var m = await db.Members.SingleAsync(x => x.UserId == userId, Ct);
            m.CardKeyVersion++;
            await db.SaveChangesAsync(Ct);
        }
        var stale = await api.CreateClient().GetFromJsonAsync<VerifyMemberCard.Response>($"/api/public/card/{token}", Json.Options, Ct);
        Assert.False(stale!.Valid);
        var forged = await api.CreateClient().GetFromJsonAsync<VerifyMemberCard.Response>("/api/public/card/abc", Json.Options, Ct);
        Assert.False(forged!.Valid);
    }

    [Fact]
    public async Task Card_pdf_is_generated()
    {
        var (member, _) = await api.CreateMemberClientAsync();

        var response = await member.GetAsync("/api/socio/card.pdf", Ct);

        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync(Ct);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private async Task<Poll> SeedPollAsync(bool secret)
    {
        var poll = new Poll
        {
            Title = "Onde vai ser o churrasco?", IsSecret = secret, IsPublished = true,
            OpensAt = api.Clock.GetUtcNow().AddDays(-1), ClosesAt = api.Clock.GetUtcNow().AddDays(3),
        };
        poll.Options.Add(new PollOption { PollId = poll.Id, Label = "Quintal", SortOrder = 1 });
        poll.Options.Add(new PollOption { PollId = poll.Id, Label = "Parque", SortOrder = 2 });
        return await api.SeedAsync(poll);
    }

    [Fact]
    public async Task Secret_vote_cannot_be_traced_back_to_the_member()
    {
        var (member, userId) = await api.CreateMemberClientAsync();
        var poll = await SeedPollAsync(secret: true);
        var option = poll.Options[0].Id;

        var before = await member.GetFromJsonAsync<List<ListMemberPolls.Item>>("/api/socio/polls", Json.Options, Ct);
        Assert.Null(before!.Single().TotalVotes); // resultado só depois de votar

        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsJsonAsync($"/api/socio/polls/{poll.Id}/vote", new VotePoll.Request(option), Json.Options, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await member.PostAsJsonAsync($"/api/socio/polls/{poll.Id}/vote", new VotePoll.Request(option), Json.Options, Ct)).StatusCode);

        await using var db = api.CreateDbContext();
        Assert.True(await db.PollParticipations.AnyAsync(p => p.UserId == userId, Ct));
        Assert.Null((await db.PollBallots.SingleAsync(Ct)).UserId);
        var after = (await member.GetFromJsonAsync<List<ListMemberPolls.Item>>("/api/socio/polls", Json.Options, Ct))!.Single();
        Assert.True(after.HasVoted);
        Assert.Null(after.MyOptionId);
        Assert.Equal(1, after.Options.Single(o => o.Id == option).Votes);
    }

    [Fact]
    public async Task Open_vote_remembers_the_choice_and_late_payers_cannot_vote()
    {
        var (member, _) = await api.CreateMemberClientAsync();
        var (late, _) = await api.CreateMemberClientAsync(active: false, name: "Atrasado");
        var poll = await SeedPollAsync(secret: false);
        var option = poll.Options[1].Id;

        await member.PostAsJsonAsync($"/api/socio/polls/{poll.Id}/vote", new VotePoll.Request(option), Json.Options, Ct);
        var lateVote = await late.PostAsJsonAsync($"/api/socio/polls/{poll.Id}/vote", new VotePoll.Request(option), Json.Options, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, lateVote.StatusCode);
        var polls = await member.GetFromJsonAsync<List<ListMemberPolls.Item>>("/api/socio/polls", Json.Options, Ct);
        Assert.Equal(option, polls!.Single().MyOptionId);
    }

    [Fact]
    public async Task Billing_reads_card_and_receipts_from_stripe()
    {
        var (member, _) = await api.CreateMemberClientAsync();
        api.Stripe.Billing = new BillingSummary("4242", [new InvoiceSummary(api.Clock.GetUtcNow(), 1500, "eur", "https://invoice.test/1")]);

        var billing = await member.GetFromJsonAsync<GetBilling.Response>("/api/socio/billing", Json.Options, Ct);
        var portal = await member.PostAsync("/api/socio/billing/portal", null, Ct);

        Assert.Equal(1500, billing!.FeeCents);
        Assert.Equal("4242", billing.CardLast4);
        Assert.Equal("https://invoice.test/1", Assert.Single(billing.Invoices).ReceiptUrl);
        Assert.Equal(HttpStatusCode.OK, portal.StatusCode);
    }

    [Fact]
    public async Task Tagged_photos_appear_and_opting_out_removes_tags()
    {
        var (member, userId) = await api.CreateMemberClientAsync();
        var photo = await api.SeedAsync(new Photo
        {
            UploadStatus = PhotoUploadStatus.Ready, Width = 100, Height = 100, AltText = "Torcida",
            Visibility = PhotoVisibility.Members,
        });
        await api.SeedAsync(new PhotoTag { PhotoId = photo.Id, UserId = userId, CreatedAt = api.Clock.GetUtcNow() });

        var mine = await member.GetFromJsonAsync<MyPhotos.Response>("/api/socio/photos", Json.Options, Ct);
        Assert.Equal(1, mine!.Total);

        var me = await member.GetFromJsonAsync<GetMemberDashboard.Response>("/api/socio/me", Json.Options, Ct);
        await member.PutAsJsonAsync("/api/socio/me",
            new UpdateMemberProfile.Request(me!.FullName, null, "DUB", PhotoTagOptIn: false, me.Version), Json.Options, Ct);

        var after = await member.GetFromJsonAsync<MyPhotos.Response>("/api/socio/photos", Json.Options, Ct);
        Assert.Equal(0, after!.Total);
    }

    [Fact]
    public async Task Documents_are_served_only_through_the_api()
    {
        var (member, _) = await api.CreateMemberClientAsync();
        api.Storage.Objects["documents/estatuto.pdf"] = "%PDF-estatuto"u8.ToArray();
        var document = await api.SeedAsync(new Document
        {
            Title = "Estatuto", StorageKey = "documents/estatuto.pdf", FileName = "estatuto.pdf",
            ContentType = "application/pdf", SizeBytes = 13, IsPublished = true,
        });

        var list = await member.GetFromJsonAsync<List<MemberDocuments.Item>>("/api/socio/documents", Json.Options, Ct);
        var file = await member.GetByteArrayAsync($"/api/socio/documents/{document.Id}/download", Ct);
        var anonymous = await api.CreateClient().GetAsync($"/api/socio/documents/{document.Id}/download", Ct);

        Assert.Equal("Estatuto", Assert.Single(list!).Title);
        Assert.Equal("%PDF-estatuto"u8.ToArray(), file);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Calendar_feed_lists_events_in_utc()
    {
        await api.CreateMemberClientAsync(); // garante a cidade
        var ev = await SeedEventAsync(new DateTimeOffset(2026, 11, 7, 0, 30, 0, TimeSpan.Zero), title: "Galo × Rival, final");

        var all = await api.CreateClient().GetStringAsync("/api/public/events.ics", Ct);
        var single = await api.CreateClient().GetAsync($"/api/public/events.ics?slug={ev.Slug}", Ct);

        Assert.Contains("DTSTART:20261107T003000Z", all);
        Assert.Contains(@"SUMMARY:Galo × Rival\, final", all);
        Assert.Equal("text/calendar", single.Content.Headers.ContentType?.MediaType);
    }
}
