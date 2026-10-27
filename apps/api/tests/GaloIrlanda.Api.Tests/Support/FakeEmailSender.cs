using System.Text.RegularExpressions;
using GaloIrlanda.Api.Integrations.Email;

namespace GaloIrlanda.Api.Tests.Support;

public sealed partial class FakeEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }

    /// <summary>Token do último link mágico enviado para <paramref name="to"/>.</summary>
    public string LastTokenFor(string to) =>
        TokenPattern().Match(Sent.Last(m => m.To == to).Text).Groups[1].Value;

    [GeneratedRegex(@"/entrar\?token=([A-Za-z0-9_-]+)")]
    private static partial Regex TokenPattern();
}
