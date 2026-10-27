namespace GaloIrlanda.Api.Integrations.Cloudflare;

/// <summary>Pede à Cloudflare um novo build do site público.</summary>
public interface IDeployHook
{
    Task TriggerAsync(CancellationToken ct);
}

public sealed class CloudflareDeployHook(HttpClient http, IConfiguration configuration, ILogger<CloudflareDeployHook> log)
    : IDeployHook
{
    public async Task TriggerAsync(CancellationToken ct)
    {
        var url = configuration["Cloudflare:DeployHookUrl"];
        if (string.IsNullOrEmpty(url))
        {
            log.LogInformation("Cloudflare:DeployHookUrl não configurada; regeração do site ignorada.");
            return;
        }

        using var response = await http.PostAsync(url, content: null, ct);
        response.EnsureSuccessStatusCode();
    }
}
