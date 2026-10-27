using GaloIrlanda.Api.Integrations.Storage;
using SkiaSharp;

namespace GaloIrlanda.Api.Features.Photos;

/// <summary>
/// Gera as variantes públicas de uma foto: corrige a orientação, descarta todos os metadados
/// (EXIF/GPS somem na recodificação) e grava WebP em 480, 1024 e 2048 px de largura, sem ampliar.
/// </summary>
public sealed class PhotoProcessor(IObjectStorage storage)
{
    public static readonly int[] Widths = [480, 1024, 2048];
    public const long MaxBytes = 20 * 1024 * 1024;
    private const long MaxPixels = 40_000_000; // protege contra "bombas de descompressão"
    private const int WebpQuality = 80;

    public static IReadOnlyList<string> Keys(Guid photoId) => Widths.Select(w => Key(photoId, w)).ToList();

    public static string Key(Guid photoId, int width) => $"photos/{photoId}/{width}.webp";

    /// <returns>Largura e altura da foto já na orientação correta, ou <c>null</c> se não for uma imagem válida.</returns>
    public async Task<(int Width, int Height)?> ProcessAsync(Guid photoId, Stream input, CancellationToken ct)
    {
        using var data = SKData.Create(input);
        using var codec = data is null ? null : SKCodec.Create(data);
        if (codec is null || (long)codec.Info.Width * codec.Info.Height > MaxPixels) return null;

        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null) return null;
        using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);

        foreach (var width in Widths)
        {
            using var variant = ResizeToWidth(oriented, width);
            using var image = SKImage.FromBitmap(variant);
            using var webp = image.Encode(SKEncodedImageFormat.Webp, WebpQuality);
            await using var stream = webp.AsStream();
            await storage.PutAsync(Key(photoId, width), stream, "image/webp", ct);
        }

        return (oriented.Width, oriented.Height);
    }

    private static SKBitmap ResizeToWidth(SKBitmap source, int width)
    {
        if (source.Width <= width) return source.Copy();
        var height = (int)Math.Round(source.Height * (width / (double)source.Width));
        return source.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
    }

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        if (origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default) return source.Copy();

        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var result = new SKBitmap(swap ? source.Height : source.Width, swap ? source.Width : source.Height);
        using var canvas = new SKCanvas(result);
        var (w, h) = (source.Width, source.Height);

        // Matrizes de https://magnushoff.com/articles/jpeg-orientation/
        var matrix = origin switch
        {
            SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
            SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
            SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
            SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
            SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
            _ => SKMatrix.Identity,
        };
        canvas.SetMatrix(matrix);
        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        return result;
    }
}
