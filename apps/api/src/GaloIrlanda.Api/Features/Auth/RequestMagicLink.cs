using System.Net;
using FluentValidation;
using GaloIrlanda.Api.Common.Validation;
using GaloIrlanda.Api.Data;
using GaloIrlanda.Api.Domain;
using GaloIrlanda.Api.Integrations.Email;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GaloIrlanda.Api.Features.Auth;

/// <summary>
/// Envia o link de acesso por e-mail. Responde igual para e-mail desconhecido, para não revelar quem
/// está cadastrado. Só usuários já cadastrados recebem link (o admin convida; sócios entram na fase 1).
/// </summary>
public static class RequestMagicLink
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private const int MaxTokensPerWindow = 3;

    public sealed record Request(string Email);

    public sealed record Response(string Message);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator() => RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
    }

    public static void Map(RouteGroupBuilder group) =>
        group.MapPost("/magic-link", Handle)
            .WithName(nameof(RequestMagicLink))
            .WithSummary("Envia um link de acesso por e-mail")
            .WithValidation<Request>()
            .RequireRateLimiting(AuthModule.RateLimitPolicy);

    private static async Task<Ok<Response>> Handle(
        Request request, AppDbContext db, IEmailSender email, IConfiguration configuration, TimeProvider clock,
        CancellationToken ct)
    {
        var response = new Response("Se o e-mail estiver cadastrado, enviamos um link de acesso. Ele vale por 15 minutos.");
        var now = clock.GetUtcNow();

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.Email == request.Email.Trim() && u.Status == UserStatus.Active, ct);
        if (user is null) return TypedResults.Ok(response);

        var recentTokens = await db.LoginTokens.CountAsync(
            t => t.UserId == user.Id && t.CreatedAt > now - TokenLifetime, ct);
        if (recentTokens >= MaxTokensPerWindow) return TypedResults.Ok(response);

        var (token, hash) = LoginTokenCodec.Create();
        db.LoginTokens.Add(new LoginToken
        {
            UserId = user.Id,
            TokenHash = hash,
            CreatedAt = now,
            ExpiresAt = now + TokenLifetime,
        });
        await db.SaveChangesAsync(ct);

        var link = $"{configuration["Site:PublicUrl"]}/entrar?token={token}";
        await email.SendAsync(Message(user, link), ct);

        return TypedResults.Ok(response);
    }

    private static EmailMessage Message(User user, string link)
    {
        var name = WebUtility.HtmlEncode(user.DisplayName);
        return new EmailMessage(
            To: user.Email,
            Subject: "Seu link de acesso ao Galo Irlanda",
            Html: $"""
                <p>Fala, {name}!</p>
                <p><a href="{link}">Clique aqui para entrar no Galo Irlanda</a>.</p>
                <p>O link vale por 15 minutos e só funciona uma vez. Se não foi você que pediu, é só ignorar.</p>
                """,
            Text: $"""
                Fala, {user.DisplayName}!

                Entre no Galo Irlanda por este link: {link}

                O link vale por 15 minutos e só funciona uma vez. Se não foi você que pediu, é só ignorar.
                """);
    }
}
