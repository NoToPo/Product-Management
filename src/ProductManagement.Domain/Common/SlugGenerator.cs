using System.Globalization;
using System.Text;

namespace ProductManagement.Domain.Common;

/// <summary>
/// Produces URL-safe slugs from arbitrary text (lower-case, ASCII, hyphenated).
/// Used to give products/categories a stable, human-readable key.
/// </summary>
public static class SlugGenerator
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Strip diacritics (e.g. "Áo Khoác" -> "Ao Khoac").
        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else if (ch is ' ' or '-' or '_' or '.')
                sb.Append('-');
            // anything else is dropped
        }

        // Collapse repeated hyphens and trim leading/trailing ones.
        var slug = sb.ToString();
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");

        return slug.Trim('-');
    }
}
