using GaloIrlanda.Api.Common.Text;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Features.Photos;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Seed;

/// <summary>
/// Cadastra o conteúdo que hoje está no layout da landing (fotos reais, eventos, notícias, locais, marcos),
/// para o admin já começar com a página montada. Textos entre colchetes continuam placeholders a editar.
/// Roda uma vez: <c>dotnet run -- seed-layout [pasta-das-fotos]</c>. Não faz nada se já houver fotos ou eventos.
/// </summary>
public static class SeedLayoutContent
{
    public const string Command = "seed-layout";

    private const string ConsentNote =
        "Importada do layout inicial (foto já publicada pela torcida). Confirmar o consentimento das pessoas que aparecem.";

    private sealed record PhotoSeed(string File, string Alt, string Title, string Caption, int? WallOrder);

    private static readonly PhotoSeed[] PhotoSeeds =
    [
        new("galo-irlanda-1.jpg", "Torcedores posando em frente a uma casa, sob a bandeira do Galo Irlanda",
            "Churrasco da Massa.", "Sol na Irlanda, acredite.", 1),
        new("galo-irlanda-2.jpg", "Torcedores do Galo e do Palmeiras com suas faixas num pub, ao lado de uma mesa de sinuca",
            "Pub dividido.", "Palmeiras de um lado, Galo do outro.", 2),
        new("galo-irlanda-3.jpg", "Torcedores do Galo Irlanda com faixa do Atlético e tambores na porta de um pub, à noite",
            "Faixa na porta do pub.", "Noite de jogo.", null),
        new("galo-irlanda-4.webp", "Grupo de atleticanos com camisas listradas em volta de uma mesa de pub",
            "Pint na mesa,", "Galo na tela.", 3),
        new("galo-irlanda-5.jpg", "Turma do Galo Irlanda reunida num quintal, de camisa do Atlético, em frente à faixa da torcida",
            "A turma reunida.", "Céu azul em Dublin.", null),
        new("galo-irlanda-6.webp", "Torcedores segurando a faixa do Galo Irlanda dentro de um pub",
            "Faixa aberta.", "Sinuca do lado, jogo na TV.", 4),
    ];

    public static async Task<string> RunAsync(IServiceProvider services, string photosPath, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var processor = scope.ServiceProvider.GetRequiredService<PhotoProcessor>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();

        if (await db.Photos.AnyAsync(ct) || await db.Events.AnyAsync(ct))
            return "Já existe conteúdo (fotos ou eventos); nada foi cadastrado.";

        await EnsureBaseDataAsync(db, now, ct);

        // Fotos
        var photos = new Dictionary<string, Photo>();
        foreach (var seed in PhotoSeeds)
        {
            var photo = new Photo
            {
                AltText = seed.Alt,
                Title = seed.Title,
                Caption = seed.Caption,
                Visibility = PhotoVisibility.Public,
                ConsentBasis = ConsentBasis.ConsentRecorded,
                ConsentNote = ConsentNote,
                ConsentConfirmedAt = now,
                WallFeatured = seed.WallOrder is not null,
                WallSortOrder = seed.WallOrder,
                UploadStatus = PhotoUploadStatus.Ready,
            };
            await using var file = File.OpenRead(Path.Combine(photosPath, seed.File));
            var size = await processor.ProcessAsync(photo.Id, file, ct)
                ?? throw new InvalidOperationException($"Não foi possível ler {seed.File}.");
            (photo.Width, photo.Height) = size;
            db.Photos.Add(photo);
            photos[seed.File] = photo;
        }

        var settings = await db.SiteSettings.SingleAsync(ct);
        settings.HeroPhotoId = photos["galo-irlanda-5.jpg"].Id;

        // Locais
        var dublinPub = new Venue { CityCode = "DUB", Name = "[Pub parceiro]", Neighborhood = "[bairro]", IsPartner = true };
        var corkPub = new Venue { CityCode = "ORK", Name = "[Pub parceiro]", IsPartner = true };
        var phoenixPark = new Venue { CityCode = "DUB", Name = "Phoenix Park" };
        db.Venues.AddRange(dublinPub, corkPub, phoenixPark);

        // Eventos do painel (datas de exemplo, como na landing)
        db.Events.AddRange(
            NewEvent(EventKind.Match, "Galo × [Adversário]", "2026-11-07T00:30:00Z", "DUB", dublinPub, EventStatus.Confirmed).Configure(e =>
            {
                e.Opponent = "[Adversário]";
                e.HomeTrip = "[linha de ônibus noturno] ou carona da Massa";
                e.SpotsTaken = 38;
                e.CoverPhotoId = photos["galo-irlanda-3.jpg"].Id;
            }),
            NewEvent(EventKind.Meetup, "Pelada da Massa", "2026-11-08T11:00:00Z", "DUB", phoenixPark, EventStatus.Confirmed).Configure(e =>
            {
                e.Capacity = 20;
                e.SpotsTaken = 14;
            }),
            NewEvent(EventKind.Match, "Galo × [Adversário]", "2026-11-14T00:30:00Z", "ORK", corkPub, EventStatus.Confirmed).Configure(e =>
                e.Opponent = "[Adversário]"),
            NewEvent(EventKind.Meetup, "Churrasco mineiro", "2026-11-15T14:00:00Z", "GWY", null, EventStatus.Voting).Configure(e =>
                e.VenueNote = "[Local a definir]"));

        // Notícias
        var categories = await db.PostCategories.ToDictionaryAsync(c => c.Slug, c => c.Id, ct);
        db.Posts.AddRange(
            NewPost("Achamos um pub que passa o Galo com som. Sim, com som.", categories["encontros"], now.AddDays(-2)),
            NewPost("Onde você estava em 2013? A Massa de Dublin conta.", categories["memoria"], now.AddDays(-5)),
            NewPost("Pão de queijo em Dublin: onde comprar polvilho sem chorar.", categories["saudade"], now.AddDays(-9)));

        // Marcos: a landing já mostra todos; o de 1950 segue com "[confirmar detalhes]".
        await db.Milestones.ForEachAsync(m => m.IsPublished = true, ct);

        await db.SaveChangesAsync(ct);
        return $"Cadastrados: {photos.Count} fotos, 3 locais, 4 eventos, 3 notícias; foto da home e marcos publicados.";
    }

    private static Event NewEvent(
        EventKind kind, string title, string startsAt, string cityCode, Venue? venue, EventStatus status)
    {
        var start = DateTimeOffset.Parse(startsAt);
        return new Event
        {
            Slug = Slug.From($"{start:yyyy-MM-dd} {title}"),
            Kind = kind,
            Title = title,
            StartsAt = start,
            CityCode = cityCode,
            VenueId = venue?.Id,
            Status = status,
            IsPublished = true,
        };
    }

    private static Post NewPost(string title, short categoryId, DateTimeOffset publishedAt) => new()
    {
        Slug = Slug.From(title),
        Title = title,
        BodyMd = "[texto da notícia]",
        CategoryId = categoryId,
        Status = PostStatus.Published,
        PublishedAt = publishedAt,
    };

    /// <summary>Cidades, categorias, configurações e marcos vêm da migration; recria se o banco foi limpo.</summary>
    private static async Task EnsureBaseDataAsync(AppDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        if (!await db.Cities.AnyAsync(ct))
        {
            db.Cities.AddRange(
                new City { Code = "DUB", Name = "Dublin", Blurb = "Sede da bagunça. Todo jogo no [Pub parceiro], pelada no Phoenix Park.", SortOrder = 1 },
                new City { Code = "ORK", Name = "Cork", Blurb = "Turma pequena e barulhenta. Jogos grandes no [Pub parceiro].", SortOrder = 2 },
                new City { Code = "GWY", Name = "Galway", Blurb = "Ainda é um grupinho. Bora crescer? Chama no grupo.", SortOrder = 3 });
        }
        if (!await db.PostCategories.AnyAsync(ct))
        {
            db.PostCategories.AddRange(
                new PostCategory { Slug = "encontros", Name = "Encontros", SortOrder = 1 },
                new PostCategory { Slug = "memoria", Name = "Memória", SortOrder = 2 },
                new PostCategory { Slug = "saudade", Name = "Saudade", SortOrder = 3 });
        }
        if (!await db.SiteSettings.AnyAsync(ct)) db.SiteSettings.Add(new SiteSettings());
        if (!await db.Milestones.AnyAsync(ct))
        {
            db.Milestones.AddRange(
                new Milestone { Year = 1908, Text = "Nasce o Clube Atlético Mineiro, em BH." },
                new Milestone { Year = 1950, Text = "Campeões do Gelo na Europa. [confirmar detalhes]" },
                new Milestone { Year = 1971, Text = "Primeiro campeão brasileiro." },
                new Milestone { Year = 2013, Text = "Libertadores. Eu acredito." },
                new Milestone { Year = 2021, Text = "Brasileiro e Copa do Brasil. A gente viu daqui." });
        }
        await db.SaveChangesAsync(ct);
    }

    private static Event Configure(this Event ev, Action<Event> configure)
    {
        configure(ev);
        return ev;
    }
}
