using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;

namespace JustDanceServer.Services;

public interface IModernJduApplicationConfigurationService
{
    ModernJduApplicationConfigurationResponse Create(
        ModernJduClientProfile profile,
        string serviceBaseUrl);
}

public sealed class ModernJduApplicationConfigurationService : IModernJduApplicationConfigurationService
{
    private static readonly string[] EnabledFeatures =
    [
        "ApplicationMetadata",
        "ApplicationUsed",
        "ClubApplication",
        "ClubDynamicPanel",
        "ClubFriends",
        "Connection",
        "ContentFiltering",
        "CreateSession",
        "EntitiesProfile",
        "EntitiesSpace",
        "Event",
        "Everything",
        "ExtendSession",
        "FriendsLookup",
        "FriendsRequest",
        "HttpClient",
        "LeaderboardMe",
        "LeaderboardProfiles",
        "LeaderboardSpaces",
        "News",
        "NotificationBroker",
        "NotificationConnection",
        "NotificationNoBroker",
        "NotificationRequestConnections",
        "NotificationSend",
        "NotificationSendBatch",
        "NotificationSendNoBroker",
        "NotificationWebsocket",
        "Populations",
        "PopulationsAutomaticFetch",
        "PrimaryStore",
        "PrimaryStoreAutomaticFetch",
        "Profiles",
        "ProfilesExternal",
        "SecondaryStore",
        "SendPopulationsInPlayerStart",
        "SendPrimaryStoreEvent",
        "Socialfeed",
        "Stats",
        "UplayFriends",
        "UplayLaunch",
        "Users",
        "UsersCreateAndLink",
        "UsersLegalOptins",
        "UsersManagement",
        "WebSocketClient"
    ];

    public ModernJduApplicationConfigurationResponse Create(
        ModernJduClientProfile profile,
        string serviceBaseUrl)
    {
        string baseUrl = NormalizeBaseUrl(serviceBaseUrl);

        return new ModernJduApplicationConfigurationResponse
        {
            Configuration = new ModernJduApplicationConfiguration
            {
                PlatformConfig = new ModernJduPlatformConfiguration
                {
                    ApplicationId = profile.ApplicationId,
                    SpaceId = profile.SpaceId,
                    Platform = profile.Platform
                },
                Custom = CreateCustomConfiguration(),
                FeaturesSwitches = EnabledFeatures
                    .Select(name => new ModernJduFeatureSwitch { Name = name })
                    .ToArray(),
                GatewayResources = CreateGatewayResources(baseUrl),
                SdkConfig = new Dictionary<string, object>
                {
                    ["remoteLogs"] = new Dictionary<string, string>
                    {
                        ["ubiservicesLogLevel"] = "None",
                        ["prodLogLevel"] = "None"
                    },
                    ["keepAliveTimeoutMin"] = 10,
                    ["timeoutSec"] = 30,
                    ["httpParam"] = new Dictionary<string, object>
                    {
                        ["timeoutParam"] = new Dictionary<string, int> { ["initialDelayMsec"] = 30_000 }
                    },
                    ["websocketParam"] = new Dictionary<string, object>
                    {
                        ["timeoutParam"] = new Dictionary<string, int> { ["initialDelayMsec"] = 30_000 }
                    }
                }
            }
        };
    }

    private static ModernJduGatewayResource[] CreateGatewayResources(string baseUrl)
    {
        string websocketBaseUrl = baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? $"wss://{baseUrl["https://".Length..]}"
            : $"ws://{baseUrl["http://".Length..]}";

        return
        [
            Resource("applicationsMetadata", $"{baseUrl}/{{version}}/applications", 1),
            Resource("configs/events", $"{baseUrl}/{{version}}/spaces/{{spaceId}}/configs/events", 2),
            Resource("events", $"{baseUrl}/{{version}}/profiles/{{profileId}}/events", 2),
            Resource("friends", $"{baseUrl}/{{version}}/profiles/me/friends", 2),
            Resource("news", $"{baseUrl}/{{version}}/profiles/me/news", 1),
            Resource("populations", $"{baseUrl}/{{version}}/profiles/me/populations", 1),
            Resource("profiles", $"{baseUrl}/{{version}}/profiles", 2),
            Resource("profiles/applications", $"{baseUrl}/{{version}}/profiles/me/applications", 2),
            Resource("profiles/entities", $"{baseUrl}/{{version}}/profiles/{{profileId}}/entities", 2),
            Resource("profiles/inventory", $"{baseUrl}/{{version}}/profiles/{{profileId}}/inventory", 1),
            Resource("profiles/notifications", $"{baseUrl}/{{version}}/profiles/{{profileId}}/notifications", 2),
            Resource("profiles/stats", $"{baseUrl}/{{version}}/profiles/{{profileId}}/stats", 1),
            Resource("spaces/entities", $"{baseUrl}/{{version}}/spaces/{{spaceId}}/entities", 2),
            Resource("spaces/items", $"{baseUrl}/{{version}}/spaces/{{spaceId}}/items", 1),
            Resource("spaces/news", $"{baseUrl}/{{version}}/spaces/news", 1),
            Resource("spaces/offers", $"{baseUrl}/{{version}}/spaces/{{spaceId}}/offers", 1),
            Resource("spaces/stats", $"{baseUrl}/{{version}}/spaces/{{spaceId}}/communitystats", 1),
            Resource("websocket/notifications", $"{websocketBaseUrl}/{{version}}/websocket", 2),
            Resource("websocket/server", websocketBaseUrl, 1)
        ];
    }

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

    private static Dictionary<string, object> CreateCustomConfiguration()
    {
        return new Dictionary<string, object>
        {
            ["resources"] = Array.Empty<object>(),
            ["featuresSwitches"] = Array.Empty<object>(),
            ["httpParam"] = CreateRetryConfiguration(20_000),
            ["websocketParam"] = CreateRetryConfiguration(30_000)
        };
    }

    private static Dictionary<string, object> CreateRetryConfiguration(int timeoutMilliseconds)
    {
        return new Dictionary<string, object>
        {
            ["maxCount"] = 0,
            ["retryParam"] = new Dictionary<string, int>
            {
                ["incrementFactorMsec"] = 0,
                ["initialDelayMsec"] = 0,
                ["randomDelayMsec"] = 0
            },
            ["timeoutParam"] = new Dictionary<string, int>
            {
                ["initialDelayMsec"] = timeoutMilliseconds,
                ["incrementFactorMsec"] = 0
            }
        };
    }

    private static ModernJduGatewayResource Resource(string name, string url, int version)
        => new() { Name = name, Url = url, Version = version };
}
