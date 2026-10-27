using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Data;

public static class DataSetup
{
    public static IServiceCollection AddGaloData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Db")
            ?? throw new InvalidOperationException("ConnectionStrings:Db não configurada.");

        services.AddSingleton<AuditingInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<AuditingInterceptor>()));

        return services;
    }
}
