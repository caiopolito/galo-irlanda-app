using System.Reflection;
using GaloIrlanda.Api.Domain;
using Microsoft.AspNetCore.DataProtection;
using QRCoder;
using SkiaSharp;

namespace GaloIrlanda.Api.Features.MemberArea;

/// <summary>
/// Carteirinha: o QR leva a um link de verificação com token assinado (Data Protection). Reemitir a
/// carteirinha (<see cref="Member.CardKeyVersion"/>) invalida QR codes antigos.
/// </summary>
public sealed class MemberCard(IDataProtectionProvider dataProtection, IConfiguration configuration)
{
    private readonly IDataProtector _protector = dataProtection.CreateProtector("GaloIrlanda.MemberCard");

    public string Token(Member member) => _protector.Protect($"{member.UserId:N}:{member.CardKeyVersion}");

    public (Guid UserId, short Version)? ReadToken(string token)
    {
        try
        {
            var parts = _protector.Unprotect(token).Split(':');
            return parts.Length == 2 && Guid.TryParse(parts[0], out var id) && short.TryParse(parts[1], out var version)
                ? (id, version)
                : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }

    public string VerificationUrl(string token) => $"{configuration["Site:PublicUrl"]}/carteirinha/{token}";

    public static string QrSvg(string content)
    {
        using var data = QRCodeGenerator.GenerateQrCode(content, QRCodeGenerator.ECCLevel.M);
        return new SvgQRCode(data).GetGraphic(4, "#121110", "#F4F1EA", drawQuietZones: true,
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);
    }

    /// <summary>Cartão de embarque em PDF (A6 deitado), com as fontes do layout.</summary>
    public static byte[] Pdf(Member member, string verificationUrl, DateTimeOffset? validUntil)
    {
        const float width = 420, height = 298; // A6 deitado, em pontos
        using var stream = new MemoryStream();
        using (var document = SKDocument.CreatePdf(stream))
        {
            var canvas = document.BeginPage(width, height);
            using var serif = LoadFont("YoungSerif-Regular.ttf");
            using var sans = LoadFont("Figtree.ttf");
            var ink = SKColor.Parse("#121110");
            var muted = SKColor.Parse("#6B665C");

            using (var paper = new SKPaint { Color = SKColor.Parse("#F4F1EA"), IsAntialias = true })
                canvas.DrawRoundRect(new SKRect(0, 0, width, height), 18, 18, paper);

            Text(canvas, "Galo Irlanda · Cartão de embarque", 24, 40, serif, 14, ink);
            DrawRoute(canvas, serif, ink);
            Label(canvas, "Passageiro", member.FullName, 24, 150, sans, muted, ink);
            Label(canvas, "Sócio nº", member.MemberNumber.ToString("0000"), 24, 200, sans, muted, ink);
            Label(canvas, "Válido até", validUntil?.ToString("dd/MM/yyyy") ?? "—", 130, 200, sans, muted, ink);
            Text(canvas, "Torcida independente. Sem vínculo oficial com o Clube Atlético Mineiro.", 24, 274, sans, 7, muted);

            using (var dash = new SKPaint
            {
                Color = SKColor.Parse("#C9C3B6"), StrokeWidth = 1.5f, IsAntialias = true, Style = SKPaintStyle.Stroke,
                PathEffect = SKPathEffect.CreateDash([5, 4], 0),
            })
                canvas.DrawLine(272, 16, 272, height - 16, dash);

            using var qrData = QRCodeGenerator.GenerateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.M);
            using var qr = SKImage.FromEncodedData(new PngByteQRCode(qrData).GetGraphic(10, [18, 17, 16], [244, 241, 234]));
            canvas.DrawImage(qr, new SKRect(286, 70, 406, 190), new SKSamplingOptions(SKFilterMode.Nearest));
            Text(canvas, "mostre no pub parceiro", 290, 212, sans, 9, muted);

            document.EndPage();
            document.Close();
        }
        return stream.ToArray();
    }

    /// <summary>"DUB ⇢ CNF": a Young Serif não tem seta, então a seta é desenhada (tracejada, como no layout).</summary>
    private static void DrawRoute(SKCanvas canvas, SKTypeface serif, SKColor ink)
    {
        const float size = 34;
        using var font = new SKFont(serif, size);
        var dubWidth = font.MeasureText("DUB");
        Text(canvas, "DUB", 24, 100, serif, size, ink);

        var start = 24 + dubWidth + 10;
        var end = start + 36;
        using var stroke = new SKPaint
        {
            Color = ink, StrokeWidth = 2, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round,
            PathEffect = SKPathEffect.CreateDash([3, 5], 0),
        };
        canvas.DrawLine(start, 88, end, 88, stroke);
        using var head = new SKPaint { Color = ink, StrokeWidth = 2, IsAntialias = true, StrokeCap = SKStrokeCap.Round };
        canvas.DrawLine(end - 7, 82, end, 88, head);
        canvas.DrawLine(end - 7, 94, end, 88, head);

        Text(canvas, "CNF", end + 10, 100, serif, size, ink);
    }

    private static void Text(SKCanvas canvas, string text, float x, float y, SKTypeface typeface, float size, SKColor color)
    {
        using var font = new SKFont(typeface, size);
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }

    private static void Label(
        SKCanvas canvas, string label, string value, float x, float y, SKTypeface typeface, SKColor muted, SKColor ink)
    {
        Text(canvas, label, x, y, typeface, 9, muted);
        Text(canvas, value, x, y + 16, typeface, 13, ink);
    }

    private static SKTypeface LoadFont(string file)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith(file, StringComparison.Ordinal));
        return SKTypeface.FromStream(assembly.GetManifestResourceStream(name)!);
    }
}
