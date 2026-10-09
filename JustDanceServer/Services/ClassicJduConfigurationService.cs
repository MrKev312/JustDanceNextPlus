using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Services;

public interface IClassicJduConfigurationService
{
    ClassicJduConfigurationResponse Create(
        ClassicJduClientProfile profile,
        string serviceBaseUrl);
}

public sealed class ClassicJduConfigurationService : IClassicJduConfigurationService
{
    private static readonly string[] SocialFeatures =
    [
        "FriendsLookup",
        "FriendsRequest",
        "Profiles",
        "UplayFriends",
        "Users"
    ];

    public ClassicJduConfigurationResponse Create(
        ClassicJduClientProfile profile,
        string serviceBaseUrl)
    {
        string baseUrl = NormalizeBaseUrl(serviceBaseUrl);

        return new ClassicJduConfigurationResponse
        {
            Configuration = new ClassicJduConfiguration
            {
                PlatformConfig = new ClassicJduPlatformConfiguration
                {
                    ApplicationId = profile.ApplicationId,
                    SpaceId = profile.SpaceId,
                    Platform = profile.Platform
                },
                FeaturesSwitches =
                [
                    .. SocialFeatures.Select(name => new ClassicJduFeatureSwitch { Name = name })
                ],
                GatewayResources =
                [
                    Resource(baseUrl, "friends", "profiles/me/friends", 2),
                    Resource(baseUrl, "profiles", "profiles", 2),
                    Resource(baseUrl, "populations", "profiles/me/populations"),
                    Resource(baseUrl, "spaces/entities", "spaces/{spaceId}/entities", 2),
                    Resource(baseUrl, "news", "profiles/me/news"),
                    Resource(baseUrl, "profiles/applications", "profiles/me/applications"),
                    Resource(baseUrl, "profiles/actions", "profiles/{profileId}/actions"),
                    Resource(baseUrl, "profiles/rewards", "profiles/{profileId}/rewards")
                ]
            }
        };
    }

    private static ClassicJduGatewayResource Resource(
        string baseUrl,
        string name,
        string path,
        int version = 1)
        => new()
        {
            Name = name,
            Url = $"{baseUrl}/{{version}}/{path}",
            Version = version
        };

    private static string NormalizeBaseUrl(string serviceBaseUrl)
    {
        string baseUrl = serviceBaseUrl.Trim().TrimEnd('/');
        if (baseUrl.Length == 0)
            throw new ArgumentException("The service base URL cannot be empty.", nameof(serviceBaseUrl));

        if (!baseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            baseUrl = $"https://{baseUrl}";
        }

        return baseUrl;
    }
}
