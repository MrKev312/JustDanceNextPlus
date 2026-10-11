using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

namespace JustDanceServer.Services;

public interface IModernJduDiscoveryService
{
    ModernJduServerEntity CreateServerEntity(Guid spaceId);
}

public sealed class ModernJduDiscoveryService(IOptions<UrlSettings> urlSettings)
    : IModernJduDiscoveryService
{
    public ModernJduServerEntity CreateServerEntity(Guid spaceId)
    {
        return new ModernJduServerEntity
        {
            EntityId = Jdu2019ClientProfiles.ServerEntityId,
            SpaceId = spaceId,
            Obj = new ModernJduServerInfo { Url = GetJduBaseUrl() }
        };
    }

    private string GetJduBaseUrl()
    {
        string configuredHost = string.IsNullOrWhiteSpace(urlSettings.Value.JduHostUrl)
            ? urlSettings.Value.HostUrl
            : urlSettings.Value.JduHostUrl;
        string host = configuredHost.Trim().TrimEnd('/');

        if (host.Length == 0)
            throw new InvalidOperationException("A JDU host URL must be configured.");

        return host.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? host
                : $"https://{host}";
    }
}
