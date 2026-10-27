namespace GaloIrlanda.Api.Common.Http;

public static class CorsSetup
{
    public const string PolicyName = "site";

    public static IServiceCollection AddGaloCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Site:Origins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(origins)
            .AllowCredentials()
            .WithHeaders("Content-Type", CsrfHeaderFilter.HeaderName)
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")));

        return services;
    }
}
