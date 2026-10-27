using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Auth;

/// <summary>
/// Cria o primeiro admin a partir de <c>ADMIN_BOOTSTRAP_EMAIL</c> (segredo do Fly) quando ainda não há
/// nenhum. Depois disso, novos usuários entram por convite no admin. Nenhum e-mail fica no repositório.
/// </summary>
public static class BootstrapAdmin
{
    public static async Task EnsureAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var email = configuration["Admin:BootstrapEmail"] ?? configuration["ADMIN_BOOTSTRAP_EMAIL"];
        if (string.IsNullOrWhiteSpace(email)) return;

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.UserRoles.AnyAsync(r => r.Role == Roles.Admin, ct)) return;

        var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            user = new User { Email = email.Trim(), DisplayName = "Admin" };
            db.Users.Add(user);
        }

        user.Roles.Add(new UserRole
        {
            UserId = user.Id,
            Role = Roles.Admin,
            GrantedAt = services.GetRequiredService<TimeProvider>().GetUtcNow(),
        });
        await db.SaveChangesAsync(ct);

        services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(BootstrapAdmin))
            .LogInformation("Primeiro admin criado a partir de ADMIN_BOOTSTRAP_EMAIL.");
    }
}
