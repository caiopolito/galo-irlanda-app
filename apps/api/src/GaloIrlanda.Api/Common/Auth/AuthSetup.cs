using GaloIrlanda.Api.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

namespace GaloIrlanda.Api.Common.Auth;

public static class AuthSetup
{
    public static IServiceCollection AddGaloAuth(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<CurrentUser>();
        services.AddDataProtection().SetApplicationName("galo-irlanda").PersistKeysToDbContext<AppDbContext>();

        // Cookie de sessão (ADR 0006/0014). A sessão fica no banco: ver Features/Auth/SessionTicketStore.
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "galo_session";
                options.Cookie.HttpOnly = true;
                // Em desenvolvimento o front roda em http://localhost; em produção, só HTTPS.
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
                // API: responder 401/403 em vez de redirecionar para uma página de login.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Admin, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(Policies.Editor, policy => policy.RequireRole(Roles.Admin, Roles.Editor));

        return services;
    }
}
