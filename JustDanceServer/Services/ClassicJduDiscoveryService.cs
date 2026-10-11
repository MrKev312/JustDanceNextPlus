using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.Extensions.Options;

namespace JustDanceServer.Services;

public interface IClassicJduDiscoveryService
{
    ClassicJduPopulation CreatePopulation(ClassicJduClientProfile profile);
    ClassicJduEntity CreateEntity(ClassicJduClientProfile profile);
}

public sealed class ClassicJduDiscoveryService(IOptions<UrlSettings> urlSettings) : IClassicJduDiscoveryService
{
    public ClassicJduPopulation CreatePopulation(ClassicJduClientProfile profile)
    {
        return new ClassicJduPopulation
        {
            Name = "prod",
            Obj = CreateServerInfo(),
            Subject = "server",
            SpaceId = profile.SpaceId,
            AssignmentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    public ClassicJduEntity CreateEntity(ClassicJduClientProfile profile)
    {
        return new ClassicJduEntity
        {
            EntityId = JduConstants.ClassicServerEntityId,
            SpaceId = profile.SpaceId,
            Obj = CreateServerInfo(),
            LastModified = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    private ClassicJduServerInfo CreateServerInfo()
    {
        string configuredHost = string.IsNullOrWhiteSpace(urlSettings.Value.JduHostUrl)
            ? urlSettings.Value.HostUrl
            : urlSettings.Value.JduHostUrl;

        string url = configuredHost.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || configuredHost.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? configuredHost.TrimEnd('/')
                : $"https://{configuredHost.TrimEnd('/')}";

        return new ClassicJduServerInfo { Url = url };
    }
}
