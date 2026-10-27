using SkiaSharp;

namespace GaloIrlanda.Api.Tests.Support;

public static class TestImages
{
    public static byte[] Jpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(SKColors.Goldenrod);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Jpeg, 90).ToArray();
    }

    public static (int Width, int Height) SizeOf(byte[] encoded)
    {
        using var codec = SKCodec.Create(new MemoryStream(encoded));
        return (codec.Info.Width, codec.Info.Height);
    }

    /// <summary>Pasta das fotos reais do layout (apps/web/src/assets/photos), achada subindo a partir dos binários.</summary>
    public static string LayoutPhotosPath()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "apps", "web", "src", "assets", "photos");
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException("apps/web/src/assets/photos não encontrada.");
    }
}
