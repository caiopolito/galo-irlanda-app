using System.Threading.RateLimiting;
using GaloIrlanda.Api.Common.Http;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GaloIrlanda.Api.Features.Auth;

public static class AuthModule
{
    public const string RateLimitPolicy = "auth";

    public static IServiceCollection AddAuthFeature(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<SessionTicketStore>();
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<SessionTicketStore>((options, store) => options.SessionStore = store);

        var permitsPerMinute = configuration.GetValue("Auth:RateLimitPerMinute", 10);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                ClientIp(context),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitsPerMinute, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }

    public static void MapAuthEndpoints(this EndpointGroups groups)
    {
        RequestMagicLink.Map(groups.Auth);
        ConsumeMagicLink.Map(groups.Auth);
        GetMe.Map(groups.Auth);
        Logout.Map(groups.Auth);
    }

    // No Fly.io o IP real do cliente vem no header Fly-Client-IP.
    private static string ClientIp(HttpContext context) =>
        context.Request.Headers["Fly-Client-IP"].FirstOrDefault()
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "desconhecido";
}
