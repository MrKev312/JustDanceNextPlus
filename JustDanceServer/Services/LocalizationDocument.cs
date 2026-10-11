using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JustDanceServer.Services;

public static class LocalizationDocument
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static byte[] Create(ILocalizedStringService text, string? locale)
    {
        using MemoryStream stream = new();
        using (GZipStream gzip = new(stream, CompressionLevel.Optimal, leaveOpen: true))
            JsonSerializer.Serialize(gzip, text.GetDatabase(locale), Options);
        return stream.ToArray();
    }
}
