using System.Security.Cryptography;
using GaloIrlanda.Api.Common.Auth;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Email;
using GaloIrlanda.Api.Integrations.Stripe;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Membership;

/// <summary>
/// "Pagou, virou sócio": cria (ou reaproveita) o usuário, a ficha de sócio com número da carteirinha,
/// o papel <c>socio</c> e o espelho da assinatura. Idempotente: rodar de novo não duplica nada.
/// Para exigir aprovação no futuro, é aqui que a regra muda.
/// </summary>
public sealed class ProvisionMember(AppDbContext db, IEmailSender email, IConfiguration configuration, TimeProvider clock)
{
    private const string ReferralAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public sealed record Input(
        string Email,
        string FullName,
        string CityCode,
        string? Phone,
        string? ReferralCode,
        string StripeCustomerId,
        string StripeSubscriptionId,
        SubscriptionState Subscription,
        DateTimeOffset EventAt);

    public async Task<Member> RunAsync(Input input, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var user = await db.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Email == input.Email, ct);
        if (user is null)
        {
            user = new User { Email = input.Email, DisplayName = FirstName(input.FullName) };
            db.Users.Add(user);
        }

        var cityCode = await db.Cities.AnyAsync(c => c.Code == input.CityCode, ct) ? input.CityCode : "DUB";
        var member = await db.Members.FirstOrDefaultAsync(m => m.UserId == user.Id, ct);
        var isNewMember = member is null;
        if (member is null)
        {
            var referrer = string.IsNullOrEmpty(input.ReferralCode)
                ? null
                : await db.Members.Where(m => m.ReferralCode == input.ReferralCode).Select(m => (Guid?)m.UserId).FirstOrDefaultAsync(ct);
            member = new Member
            {
                UserId = user.Id,
                FullName = input.FullName,
                Phone = string.IsNullOrEmpty(input.Phone) ? null : input.Phone,
                CityCode = cityCode,
                JoinedAt = now,
                ReferralCode = NewReferralCode(),
                ReferredBy = referrer,
            };
            db.Members.Add(member);
        }

        if (user.Roles.All(r => r.Role != Roles.Socio))
            user.Roles.Add(new UserRole { UserId = user.Id, Role = Roles.Socio, GrantedAt = now });
        if (user.Status == UserStatus.Disabled) user.Status = UserStatus.Active;

        var subscription = await db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == input.StripeSubscriptionId, ct);
        if (subscription is null)
        {
            db.Subscriptions.Add(new Subscription
            {
                UserId = user.Id,
                StripeCustomerId = input.StripeCustomerId,
                StripeSubscriptionId = input.StripeSubscriptionId,
                Status = input.Subscription.Status,
                CurrentPeriodEnd = input.Subscription.CurrentPeriodEnd,
                CancelAtPeriodEnd = input.Subscription.CancelAtPeriodEnd,
                LastEventAt = input.EventAt,
            });
        }

        await db.SaveChangesAsync(ct);

        if (isNewMember) await SendWelcomeAsync(user, member, ct);
        return member;
    }

    private async Task SendWelcomeAsync(User user, Member member, CancellationToken ct)
    {
        var link = $"{configuration["Site:PublicUrl"]}/entrar";
        var number = member.MemberNumber.ToString("0000");
        await email.SendAsync(new EmailMessage(
            To: user.Email,
            Subject: $"Bem-vindo à Massa, sócio nº {number}!",
            Html: $"""
                <p>Fala, {System.Net.WebUtility.HtmlEncode(user.DisplayName)}!</p>
                <p>Pagamento confirmado: agora você é o sócio nº <strong>{number}</strong> do Galo Irlanda.</p>
                <p>Sua carteirinha já está na área do sócio: acesse <a href="{link}">{link}</a> e entre com este e-mail.</p>
                """,
            Text: $"""
                Fala, {user.DisplayName}!

                Pagamento confirmado: agora você é o sócio nº {number} do Galo Irlanda.
                Sua carteirinha já está na área do sócio: acesse {link} e entre com este e-mail.
                """), ct);
    }

    private static string FirstName(string fullName) => fullName.Trim().Split(' ', 2)[0];

    private static string NewReferralCode() =>
        string.Concat(Enumerable.Range(0, 8).Select(_ => ReferralAlphabet[RandomNumberGenerator.GetInt32(ReferralAlphabet.Length)]));
}
