using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace GaloIrlanda.Api.Integrations.Email;

public sealed record EmailMessage(string To, string Subject, string Html, string Text);

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public sealed class EmailOptions
{
    public string From { get; set; } = "Galo Irlanda <nao-responda@localhost>";
    public string? ResendApiKey { get; set; }
}

/// <summary>Envio pela API do Resend (https://resend.com/docs/api-reference/emails/send-email).</summary>
public sealed class ResendEmailSender(HttpClient http, IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = JsonContent.Create(new
            {
                from = options.Value.From,
                to = new[] { message.To },
                subject = message.Subject,
                html = message.Html,
                text = message.Text,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ResendApiKey);

        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}

/// <summary>Só em desenvolvimento: escreve o e-mail no log (inclusive o link de acesso).</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> log) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        log.LogWarning("E-mail de desenvolvimento para {To} — {Subject}\n{Text}", message.To, message.Subject, message.Text);
        return Task.CompletedTask;
    }
}

public static class EmailSetup
{
    public static IServiceCollection AddEmail(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("Email");
        services.Configure<EmailOptions>(section);

        if (!string.IsNullOrEmpty(section["ResendApiKey"]))
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>();
        }
        else if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            // Em produção, logar o e-mail vazaria links de acesso.
            throw new InvalidOperationException("Email:ResendApiKey não configurada.");
        }

        return services;
    }
}
