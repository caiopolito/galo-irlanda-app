using System.Globalization;
using System.Text;

namespace GaloIrlanda.Api.Common.Text;

public static class Slug
{
    private const int MaxLength = 80;

    /// <summary>"Galo × Adversário 07/11" → "galo-adversario-07-11".</summary>
    public static string From(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var pendingDash = false;

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsAsciiLetterOrDigit(c))
            {
                if (pendingDash && builder.Length > 0) builder.Append('-');
                builder.Append(char.ToLowerInvariant(c));
                pendingDash = false;
            }
            else
            {
                pendingDash = true;
            }

            if (builder.Length >= MaxLength) break;
        }

        return builder.ToString().TrimEnd('-');
    }
}
