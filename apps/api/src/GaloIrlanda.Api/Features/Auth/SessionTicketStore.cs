using System.Security.Claims;
using System.Security.Cryptography;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Auth;

/// <summary>
/// Guarda a sessão do cookie na tabela <c>sessions</c> (ADR 0014). O cookie só leva uma chave opaca;
/// assim dá para revogar sessões e bloquear um usuário na hora. Os papéis são relidos do banco a cada
/// requisição, então mudanças de papel valem imediatamente.
/// </summary>
public sealed class SessionTicketStore(
    IServiceScopeFactory scopes, IDataProtectionProvider dataProtection, TimeProvider clock) : ITicketStore
{
    public static readonly TimeSpan SlidingExpiration = TimeSpan.FromDays(30);
    public static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromDays(90);
    private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(5);

    private readonly IDataProtector _protector = dataProtection.CreateProtector("GaloIrlanda.Sessions");

    public Task<string> StoreAsync(AuthenticationTicket ticket) => StoreAsync(ticket, null, CancellationToken.None);

    public async Task<string> StoreAsync(AuthenticationTicket ticket, HttpContext? httpContext, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (key, hash) = LoginTokenCodec.Create();
        var now = clock.GetUtcNow();
        var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Sessions.Add(new Session
        {
            UserId = userId,
            TokenHash = hash,
            Ticket = Protect(ticket),
            UserAgent = string.IsNullOrEmpty(userAgent) ? null : userAgent[..Math.Min(userAgent.Length, 200)],
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now + SlidingExpiration,
        });
        await db.SaveChangesAsync(cancellationToken);
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (!LoginTokenCodec.TryHash(key, out var hash)) return;

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await db.Sessions.FirstOrDefaultAsync(s => s.TokenHash == hash && s.RevokedAt == null);
        if (session is null) return;

        var now = clock.GetUtcNow();
        session.Ticket = Protect(ticket);
        session.LastSeenAt = now;
        session.ExpiresAt = NextExpiry(session, now);
        await db.SaveChangesAsync();
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        if (!LoginTokenCodec.TryHash(key, out var hash)) return null;

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = clock.GetUtcNow();

        var session = await db.Sessions
            .FirstOrDefaultAsync(s => s.TokenHash == hash && s.RevokedAt == null && s.ExpiresAt > now);
        if (session is null) return null;

        var user = await db.Users
            .Where(u => u.Id == session.UserId && u.Status == UserStatus.Active)
            .Select(u => new { u.Id, u.DisplayName, Roles = u.Roles.Select(r => r.Role).ToList() })
            .FirstOrDefaultAsync();
        if (user is null) return null;

        var stored = Unprotect(session.Ticket);
        if (stored is null) return null;

        if (now - session.LastSeenAt > TouchInterval)
        {
            session.LastSeenAt = now;
            session.ExpiresAt = NextExpiry(session, now);
            await db.SaveChangesAsync();
        }

        return new AuthenticationTicket(
            UserPrincipal.Create(user.Id, user.DisplayName, user.Roles),
            stored.Properties,
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task RemoveAsync(string key)
    {
        if (!LoginTokenCodec.TryHash(key, out var hash)) return;

        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Sessions
            .Where(s => s.TokenHash == hash && s.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, clock.GetUtcNow()));
    }

    private static DateTimeOffset NextExpiry(Session session, DateTimeOffset now)
    {
        var sliding = now + SlidingExpiration;
        var absolute = session.CreatedAt + AbsoluteExpiration;
        return sliding < absolute ? sliding : absolute;
    }

    private byte[] Protect(AuthenticationTicket ticket) => _protector.Protect(TicketSerializer.Default.Serialize(ticket));

    private AuthenticationTicket? Unprotect(byte[] data)
    {
        try
        {
            return TicketSerializer.Default.Deserialize(_protector.Unprotect(data));
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
