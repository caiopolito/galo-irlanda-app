using System.Collections.Concurrent;
using GaloIrlanda.Api.Integrations.Storage;

namespace GaloIrlanda.Api.Tests.Support;

public sealed class InMemoryObjectStorage : IObjectStorage
{
    public ConcurrentDictionary<string, byte[]> Objects { get; } = new();

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        Objects[key] = buffer.ToArray();
    }

    public Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken ct)
    {
        foreach (var key in keys) Objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) =>
        Task.FromResult<Stream?>(Objects.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);

    public string PublicUrl(string key) => "https://fotos.teste/" + key;
}
