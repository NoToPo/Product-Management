namespace ProductManagement.Application.Common;

/// <summary>
/// Maps the entity concurrency token (<c>uint</c> xmin) to/from a weak-free HTTP
/// ETag string (e.g. <c>"42"</c>). Centralised so request/response sides agree.
/// </summary>
public static class ETag
{
    public static string From(uint version) => $"\"{version}\"";

    /// <summary>Parses an ETag or raw version header into a version number.</summary>
    public static bool TryParse(string? value, out uint version)
    {
        version = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var trimmed = value.Trim().Trim('"');
        return uint.TryParse(trimmed, out version);
    }
}
