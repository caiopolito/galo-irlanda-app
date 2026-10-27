using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Storage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Landing;

/// <summary>
/// Tudo que a landing precisa numa chamada, lida no build do site. Só conteúdo publicado e fotos
/// públicas. Fotos vêm como <c>BaseUrl</c> + <c>{largura}.webp</c> (480, 1024, 2048).
/// </summary>
public static class GetLanding
{
    private const int BoardLimit = 6;
    private const int NewsLimit = 3;
    private const int WallLimit = 8;
    private static readonly TimeSpan InProgressWindow = TimeSpan.FromHours(3);

    public sealed record Response(
        PhotoRef? Hero,
        IReadOnlyList<BoardEvent> Events,
        NextMatch? NextMatch,
        IReadOnlyList<CityCard> Cities,
        IReadOnlyList<NewsItem> News,
        IReadOnlyList<WallPhoto> WallPhotos,
        IReadOnlyList<MilestoneItem> Milestones,
        SiteLinks Settings);

    public sealed record PhotoRef(string BaseUrl, int Width, int Height, string Alt);

    public sealed record BoardEvent(
        Guid Id, string Slug, EventKind Kind, string Title, DateTimeOffset StartsAt, string Place, string CityCode,
        string CityName, EventStatus Status, int? Capacity, int SpotsTaken);

    public sealed record NextMatch(
        string Slug, string Title, DateTimeOffset StartsAt, string Place, string? HomeTrip, int Confirmed, PhotoRef? Photo);

    public sealed record CityCard(string Code, string Name, string? Blurb, PhotoRef? Photo);

    public sealed record NewsItem(string Slug, string? Category, DateTimeOffset PublishedAt, string Title, PhotoRef? Cover);

    public sealed record WallPhoto(PhotoRef Photo, string? Title, string? Caption);

    public sealed record MilestoneItem(short Year, string Text);

    public sealed record SiteLinks(
        int? MembershipFeeCents, string MembershipCurrency, string? WhatsappUrl, string? InstagramUrl, string? ContactEmail);

    public static void Map(RouteGroupBuilder group) =>
        group.MapGet("/landing", Handle).WithName(nameof(GetLanding)).WithSummary("Conteúdo da página inicial");

    private static async Task<Ok<Response>> Handle(
        AppDbContext db, IObjectStorage storage, TimeProvider clock, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var since = now - InProgressWindow;

        // Fotos públicas indexadas por id; cada seção só guarda o id.
        var publicPhotos = await db.Photos
            .Where(p => p.Visibility == PhotoVisibility.Public && p.DeletedAt == null && p.UploadStatus == PhotoUploadStatus.Ready)
            .Select(p => new { p.Id, p.Width, p.Height, p.AltText, p.Title, p.Caption, p.WallFeatured, p.WallSortOrder })
            .ToListAsync(ct);
        var photos = publicPhotos.ToDictionary(
            p => p.Id, p => new PhotoRef(storage.PhotoBaseUrl(p.Id), p.Width!.Value, p.Height!.Value, p.AltText!));
        PhotoRef? Photo(Guid? id) => id is { } value ? photos.GetValueOrDefault(value) : null;

        var upcoming = db.Events.Where(e => e.IsPublished && e.Status != EventStatus.Cancelled && e.StartsAt >= since);

        var events = await upcoming
            .OrderBy(e => e.StartsAt)
            .Take(BoardLimit)
            .Select(e => new BoardEvent(e.Id, e.Slug, e.Kind, e.Title, e.StartsAt,
                e.Venue != null ? e.Venue.Name : e.VenueNote ?? "", e.CityCode, e.City.Name, e.Status, e.Capacity,
                e.SpotsTaken + db.EventRsvps.Count(r => r.EventId == e.Id && r.Answer == RsvpAnswer.Going)))
            .ToListAsync(ct);

        var match = await upcoming
            .Where(e => e.Kind == EventKind.Match)
            .OrderBy(e => e.StartsAt)
            .Select(e => new
            {
                e.Slug, e.Title, e.StartsAt, e.HomeTrip, e.CoverPhotoId,
                SpotsTaken = e.SpotsTaken + db.EventRsvps.Count(r => r.EventId == e.Id && r.Answer == RsvpAnswer.Going),
                Place = e.Venue != null
                    ? (e.Venue.Neighborhood != null ? e.Venue.Name + ", " + e.Venue.Neighborhood : e.Venue.Name)
                    : e.VenueNote ?? "",
            })
            .FirstOrDefaultAsync(ct);

        var cities = await db.Cities
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new { c.Code, c.Name, c.Blurb, c.PhotoId })
            .ToListAsync(ct);

        var news = await db.Posts
            .Where(p => p.Status == PostStatus.Published && p.PublishedAt <= now)
            .OrderByDescending(p => p.PublishedAt)
            .Take(NewsLimit)
            .Select(p => new { p.Slug, Category = p.Category != null ? p.Category.Name : null, PublishedAt = p.PublishedAt!.Value, p.Title, p.CoverPhotoId })
            .ToListAsync(ct);

        var milestones = await db.Milestones
            .Where(m => m.IsPublished)
            .OrderBy(m => m.Year).ThenBy(m => m.SortOrder)
            .Select(m => new MilestoneItem(m.Year, m.Text))
            .ToListAsync(ct);

        var settings = await db.SiteSettings.SingleAsync(ct);

        var wall = publicPhotos
            .Where(p => p.WallFeatured)
            .OrderBy(p => p.WallSortOrder)
            .Take(WallLimit)
            .Select(p => new WallPhoto(photos[p.Id], p.Title, p.Caption))
            .ToList();

        return TypedResults.Ok(new Response(
            Hero: Photo(settings.HeroPhotoId),
            Events: events,
            NextMatch: match is null ? null
                : new NextMatch(match.Slug, match.Title, match.StartsAt, match.Place, match.HomeTrip, match.SpotsTaken, Photo(match.CoverPhotoId)),
            Cities: cities.Select(c => new CityCard(c.Code, c.Name, c.Blurb, Photo(c.PhotoId))).ToList(),
            News: news.Select(n => new NewsItem(n.Slug, n.Category, n.PublishedAt, n.Title, Photo(n.CoverPhotoId))).ToList(),
            WallPhotos: wall,
            Milestones: milestones,
            Settings: new SiteLinks(settings.MembershipFeeCents, settings.MembershipCurrency, settings.WhatsappUrl,
                settings.InstagramUrl, settings.ContactEmail)));
    }
}
