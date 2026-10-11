using JustDanceServer.Configuration;

namespace JustDanceServer.Services;

internal static class JduContentUrlResolver
{
    public static Dictionary<string, string> RebaseAssets(
        IReadOnlyDictionary<string, string> assets,
        UrlSettings urlSettings)
        => assets.ToDictionary(
            asset => asset.Key,
            asset => RebaseToCdn(asset.Value, urlSettings),
            StringComparer.Ordinal);

    public static string RebaseToCdn(string sourceUrl, UrlSettings urlSettings)
    {
        string configuredHost = urlSettings.CDNUrl;
        string baseUrl = configuredHost.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || configuredHost.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? configuredHost
                : $"https://{configuredHost}";

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out Uri? sourceUri)
            || !Uri.TryCreate(baseUrl.TrimEnd('/') + '/', UriKind.Absolute, out Uri? jduBaseUri))
        {
            throw new InvalidOperationException(
                $"Cannot rebase JDU asset URL '{sourceUrl}' to '{configuredHost}'.");
        }

        return new Uri(jduBaseUri, sourceUri.PathAndQuery.TrimStart('/')).AbsoluteUri;
    }

    public static string GetCdnBaseUrl(UrlSettings urlSettings)
    {
        string configuredHost = urlSettings.CDNUrl.Trim().TrimEnd('/');
        return configuredHost.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || configuredHost.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? configuredHost
                : $"https://{configuredHost}";
    }
}
