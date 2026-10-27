using GaloIrlanda.Api.Integrations.Cloudflare;

namespace GaloIrlanda.Api.Tests.Support;

public sealed class FakeDeployHook : IDeployHook
{
    public int Calls { get; set; }

    public Task TriggerAsync(CancellationToken ct)
    {
        Calls++;
        return Task.CompletedTask;
    }
}
