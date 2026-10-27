using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace GaloIrlanda.Api.Integrations.Storage;

/// <summary>Arquivos públicos (variantes das fotos). R2 em produção, pasta local em desenvolvimento.</summary>
public interface IObjectStorage
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken ct);
    Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken ct);
    string PublicUrl(string key);
}

public sealed class StorageOptions
{
    /// <summary><c>Local</c> ou <c>R2</c>.</summary>
    public string Provider { get; set; } = "Local";
    /// <summary>Endereço público dos arquivos, terminando em "/" (ex.: https://fotos.dominio/ ou http://localhost:5137/media/).</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5137/media/";
    public string LocalPath { get; set; } = ".data/media";
    public string? R2AccountId { get; set; }
    public string? R2AccessKeyId { get; set; }
    public string? R2SecretAccessKey { get; set; }
    public string? R2Bucket { get; set; }
}

public sealed class R2ObjectStorage(IAmazonS3 s3, IOptions<StorageOptions> options) : IObjectStorage
{
    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.Value.R2Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            Headers = { CacheControl = "public, max-age=31536000, immutable" },
            DisablePayloadSigning = true, // o R2 não aceita payload assinado em streaming
        }, ct);
    }

    public async Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken ct)
    {
        if (keys.Count == 0) return;
        await s3.DeleteObjectsAsync(new DeleteObjectsRequest
        {
            BucketName = options.Value.R2Bucket,
            Objects = keys.Select(k => new KeyVersion { Key = k }).ToList(),
        }, ct);
    }

    public string PublicUrl(string key) => options.Value.PublicBaseUrl + key;
}

public sealed class LocalObjectStorage(IOptions<StorageOptions> options, IHostEnvironment environment) : IObjectStorage
{
    public string Root => Path.GetFullPath(Path.Combine(environment.ContentRootPath, options.Value.LocalPath));

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        var path = Path.Combine(Root, key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = File.Create(path);
        await content.CopyToAsync(file, ct);
    }

    public Task DeleteAsync(IReadOnlyCollection<string> keys, CancellationToken ct)
    {
        foreach (var key in keys) File.Delete(Path.Combine(Root, key));
        return Task.CompletedTask;
    }

    public string PublicUrl(string key) => options.Value.PublicBaseUrl + key;
}

public static class StorageSetup
{
    public static IServiceCollection AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Storage");
        services.Configure<StorageOptions>(section);

        if (string.Equals(section["Provider"], "R2", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var o = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
                return new AmazonS3Client(
                    new BasicAWSCredentials(o.R2AccessKeyId, o.R2SecretAccessKey),
                    new AmazonS3Config
                    {
                        ServiceURL = $"https://{o.R2AccountId}.r2.cloudflarestorage.com",
                        ForcePathStyle = true,
                        AuthenticationRegion = "auto",
                        // O SDK v4 manda checksums que o R2 nem sempre aceita.
                        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
                    });
            });
            services.AddSingleton<IObjectStorage, R2ObjectStorage>();
        }
        else
        {
            services.AddSingleton<LocalObjectStorage>();
            services.AddSingleton<IObjectStorage>(sp => sp.GetRequiredService<LocalObjectStorage>());
        }

        return services;
    }

    /// <summary>Em desenvolvimento, serve a pasta local em <c>/media</c>.</summary>
    public static void UseLocalMedia(this WebApplication app)
    {
        if (app.Services.GetService<LocalObjectStorage>() is not { } local) return;

        Directory.CreateDirectory(local.Root);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(local.Root),
            RequestPath = "/media",
        });
    }
}

public static class PhotoUrls
{
    /// <summary>Base das variantes de uma foto: o front completa com <c>{largura}.webp</c> (480, 1024, 2048).</summary>
    public static string PhotoBaseUrl(this IObjectStorage storage, Guid photoId) => storage.PublicUrl($"photos/{photoId}/");
}
