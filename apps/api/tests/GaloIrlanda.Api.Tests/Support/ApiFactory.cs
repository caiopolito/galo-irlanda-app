using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Common.Http;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Cloudflare;
using GaloIrlanda.Api.Integrations.Email;
using GaloIrlanda.Api.Integrations.Storage;
using GaloIrlanda.Api.Integrations.Stripe;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace GaloIrlanda.Api.Tests.Support;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private Respawner _respawner = null!;

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));
    public FakeDeployHook DeployHook { get; } = new();
    public FakeEmailSender Email { get; } = new();
    public InMemoryObjectStorage Storage { get; } = new();
    public FakeStripeGateway Stripe { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        await using (var db = CreateDbContext())
        {
            await db.Database.MigrateAsync();
        }

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            // As chaves do Data Protection ficam: o app já as carregou em memória.
            TablesToIgnore = ["__EFMigrationsHistory", "data_protection_keys"],
        });
    }

    /// <summary>Limpa o banco (inclusive o seed da migration) e os fakes. Chame no início de cada teste.</summary>
    public async ValueTask ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
        DeployHook.Calls = 0;
        Email.Sent.Clear();
        Storage.Objects.Clear();
        Stripe.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Db", _postgres.GetConnectionString());
        builder.UseSetting("Internal:Secret", "segredo-de-teste");
        builder.UseSetting("Site:PublicUrl", "https://site.teste");
        builder.UseSetting("Auth:RateLimitPerMinute", "1000");
        builder.UseSetting("Stripe:SecretKey", "sk_test_teste");
        builder.UseSetting("Stripe:WebhookSecret", StripeWebhooks.Secret);
        builder.UseSetting("Stripe:PriceId", "price_teste");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IDeployHook>(DeployHook);
            services.AddSingleton<IEmailSender>(Email);
            services.AddSingleton<IObjectStorage>(Storage);
            services.AddSingleton<IStripeGateway>(Stripe);
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
        });
    }

    /// <summary>
    /// Cliente com o header anti-CSRF. Com papel, cria o usuário no banco (as colunas created_by/updated_by
    /// têm FK para users) e autentica como ele; <c>null</c> = anônimo.
    /// </summary>
    public async Task<HttpClient> CreateClientAsAsync(string? role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");
        if (role is null) return client;

        var user = new User { Email = $"{Guid.NewGuid():N}@teste.local", DisplayName = $"Teste {role}" };
        user.Roles.Add(new UserRole { UserId = user.Id, Role = role });
        await SeedAsync(user);

        client.DefaultRequestHeaders.Add(TestAuthHandler.HeaderName, $"{user.Id}:{role}");
        return client;
    }

    /// <summary>Cliente "navegador": HTTPS (o cookie é Secure) e guarda cookies entre requisições.</summary>
    public HttpClient CreateBrowserClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });
        client.DefaultRequestHeaders.Add(CsrfHeaderFilter.HeaderName, "1");
        return client;
    }

    /// <summary>Como <see cref="CreateClientAsAsync"/>, mas devolve também o id do usuário criado.</summary>
    public async Task<(HttpClient Client, Guid UserId)> CreateClientWithUserAsync(string role)
    {
        var client = await CreateClientAsAsync(role);
        var header = client.DefaultRequestHeaders.GetValues(TestAuthHandler.HeaderName).Single();
        return (client, Guid.Parse(header.Split(':')[0]));
    }

    /// <summary>Cliente de um sócio (papel, ficha e assinatura). <paramref name="active"/>: mensalidade em dia.</summary>
    public async Task<(HttpClient Client, Guid UserId)> CreateMemberClientAsync(bool active = true, string name = "Maria da Silva")
    {
        await using (var db = CreateDbContext())
        {
            if (!await db.Cities.AnyAsync(c => c.Code == "DUB")) db.Cities.Add(new City { Code = "DUB", Name = "Dublin" });
            if (!await db.SiteSettings.AnyAsync()) db.SiteSettings.Add(new SiteSettings { MembershipFeeCents = 1500 });
            await db.SaveChangesAsync();
        }

        var (client, userId) = await CreateClientWithUserAsync(Roles.Socio);
        await using (var db = CreateDbContext())
        {
            db.Members.Add(new Member
            {
                UserId = userId, FullName = name, CityCode = "DUB", JoinedAt = Clock.GetUtcNow(),
                ReferralCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(), PhotoTagOptIn = true,
            });
            db.Subscriptions.Add(new Subscription
            {
                UserId = userId, StripeCustomerId = $"cus_{userId:N}", StripeSubscriptionId = $"sub_{userId:N}",
                Status = active ? "active" : "past_due", CurrentPeriodEnd = Clock.GetUtcNow().AddDays(20),
            });
            await db.SaveChangesAsync();
        }
        return (client, userId);
    }

    public AppDbContext CreateDbContext() =>
        Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();

    public async Task<T> SeedAsync<T>(T entity) where T : class
    {
        await using var db = CreateDbContext();
        db.Add(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
