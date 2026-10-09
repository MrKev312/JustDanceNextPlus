namespace JustDanceServer.Services;

internal static class JduSongColorProjection
{
    private static readonly IReadOnlyDictionary<string, string> FieldsByMetadataKey =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["songcolor1a"] = "songColor_1A",
            ["songcolor1b"] = "songColor_1B",
            ["songcolor2a"] = "songColor_2A",
            ["songcolor2b"] = "songColor_2B"
        };

    public static Dictionary<string, string> Create(IReadOnlyDictionary<string, string> source)
    {
        if (source.Count == 0)
            return [];

        Dictionary<string, string> normalizedSource = [with(StringComparer.OrdinalIgnoreCase)];
        foreach ((string key, string value) in source)
            normalizedSource[NormalizeKey(key)] = value;
        Dictionary<string, string> result = [with(StringComparer.Ordinal)];

        foreach ((string metadataKey, string songDbKey) in FieldsByMetadataKey)
        {
            result[songDbKey] = normalizedSource.TryGetValue(metadataKey, out string? value)
                ? NormalizeRgba(value)
                : "FFFFFFFF";
        }

        return result;
    }

    private static string NormalizeKey(string key)
        => new(key.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string NormalizeRgba(string color)
    {
        string normalized = color.Trim().TrimStart('#').ToUpperInvariant();
        return normalized.Length == 8 && normalized.All(Uri.IsHexDigit)
            ? normalized
            : "FFFFFFFF";
    }
}
