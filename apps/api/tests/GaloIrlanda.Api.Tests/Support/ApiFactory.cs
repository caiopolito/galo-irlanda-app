using GaloIrlanda.Api.Common.Http;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Cloudflare;
using GaloIrlanda.Api.Integrations.Email;
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
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Db", _postgres.GetConnectionString());
        builder.UseSetting("Internal:Secret", "segredo-de-teste");
        builder.UseSetting("Site:PublicUrl", "https://site.teste");
        builder.UseSetting("Auth:RateLimitPerMinute", "1000");
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<TimeProvider>(Clock);
            services.AddSingleton<IDeployHook>(DeployHook);
            services.AddSingleton<IEmailSender>(Email);
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
