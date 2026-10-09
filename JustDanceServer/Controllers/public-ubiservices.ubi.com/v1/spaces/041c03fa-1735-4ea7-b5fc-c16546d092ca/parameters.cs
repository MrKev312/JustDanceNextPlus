using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._041c03fa_1735_4ea7_b5fc_c16546d092ca;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("v1/spaces/{spaceId:jdu}/parameters")]
public class Parameters(IOptions<UrlSettings> urlSettings) : ControllerBase
{
    [HttpGet]
    public IActionResult GetParameters([FromRoute] Guid spaceId)
    {
        string host = string.IsNullOrWhiteSpace(urlSettings.Value.JduHostUrl)
            ? urlSettings.Value.HostUrl
            : urlSettings.Value.JduHostUrl;

        return Ok(new ParameterList
        {
            Parameters = new Dictionary<string, Parameter>
            {
                ["jd-serverInfo"] = Group(
                    new RelatedPopulation { Name = "PROD", Subject = "Environment" },
                    ("url", $"https://{host}"),
                    ("name", "PROD")),
                ["jd-customFeatureSwitches"] = Group(
                    ("relatedSongs", false),
                    ("anthologyMode", true),
                    ("customRecoLine", false)),
                ["jd-constantsFriends"] = ObjectGroup("FriendsUGC", "FriendsPresence", "FriendListService"),
                ["jd-constantsHome"] = Group(("Fetch", new { played_maps_count = 2, new_session_tiles_count = 3, during_session_tiles_count = 1 })),
                ["jd-constantsHttp"] = Group(("FileStreaming", new { slow_bit_rate_in_bps = 512000 })),
                ["jd-constantsJDVersion"] = ObjectGroup("Override"),
                ["jd-constantsQuest"] = Group(
                    ("minimumScore", 0),
                    ("questOverride", new { }),
                    ("sessionCountUntilQuestKill", 0),
                    ("sessionCountUntilDiscoveryKill", 0),
                    ("sessionCountUntilFirstDiscoveryKill", 0)),
                ["jd-constantsQuickplayDesignRules"] = ObjectGroup(
                    "FatigueRule1Config",
                    "FatigueRule2Config",
                    "RulesNamesToEnable",
                    "DiscoveryRuleConfig",
                    "DifficultyRuleConfig",
                    "MultiplayerRuleConfig",
                    "StartDifficultyRuleConfig",
                    "TaggedDiscoveryRuleConfig"),
                ["jd-constantsQuickplayFallbackLists"] = Group(("EditoFallbackList", Array.Empty<string>())),
                ["jd-constantsQuickplayTimer"] = Group(("FeatureSwitch", false), ("TimerDuration", 0)),
                ["jd-constantsRecommendation"] = Group(
                    ("Fetch", new { retry_interval = 120 }),
                    ("OnlineRecoConfig", new { numberOfSongsPlayedInPartyModeToSwitchToClassicReco = 2 }),
                    ("EditorialRecoList", new { maps = Array.Empty<string>() }),
                    ("RecoLineConfigForNonSubscribers", new { maxJDUSongsInRecoLine = 0 })),
                ["jd-constantsRelatedSongs"] = Group(
                    ("Fetch", new { retry_value = 120 }),
                    ("Patterns", new { Subscribed = Array.Empty<string>(), Unsubscribed = Array.Empty<string>() }),
                    ("LocalHistory", new { maxSizePerSession = 0 }),
                    ("minimumSongsToDisplayScreen", new { value = 0 })),
                ["jd-constantsRelatedSongsAutoplay"] = Group(("featureSwitch", false)),
                ["jd-constantsRelatedSongsAutoplayTimer"] = Group(("durationInSeconds", 0)),
                ["jd-constantsSongsSortingByRecommendation"] = Group(("isActive", false)),
                ["jd-constantsSubscription_Service"] = Group(
                    ("ECTokenFetch", new { retry_count = 0, retry_interval = 0 }),
                    ("ServerRefresh", new { retry_interval = 60, refresh_interval = 600, retry_interval_s2s = 600 })),
                ["jd-constantsUnlockable"] = ObjectGroup("AAAMap"),
                ["jd-constantsUpsell"] = ObjectGroup("streamedVideo"),
                ["jd-constantsWall"] = ObjectGroup("FriendsWall"),
                ["jd-constantsWDF"] = ObjectGroup("Recap", "UpdateScore"),
                ["us-GlobalSpaceConfig"] = Group(
                    ("friendsSpaceId", UbisoftServicesConstants.SocialSpaceId),
                    ("blocklistSpaceId", UbisoftServicesConstants.SocialSpaceId)),
                ["us-sdkClientFeaturesSwitches"] = Group(
                    ("event", true),
                    ("profiles", true),
                    ("parameters", true),
                    ("populations", true),
                    ("eventsDefinitions", true),
                    ("telemetry", false),
                    ("webSocketClient", false)),
                ["us-sdkClientMocks"] = new(),
                ["us-sdkClientNotificationsGame"] = Group(("additionalSpaces", Array.Empty<object>())),
                ["us-sdkClientNotificationsInternal"] = new(),
                ["us-sdkClientNotificationsSpaceIds"] = new(),
                ["us-sdkClientRemoteLogsGame"] = Group(("url", string.Empty), ("maxTextLength", 0)),
                ["us-sdkClientRemoteLogsInternal"] = new(),
                ["us-sdkClientSettings"] = new(),
                ["us-sdkClientSettingsCacheTTL"] = new(),
                ["us-sdkClientSettingsHttpGame"] = new(),
                ["us-sdkClientSettingsHttpInternal"] = new(),
                ["us-sdkClientSettingsSecondaryStoreSync"] = new(),
                ["us-sdkClientSettingsWebSocketGame"] = new(),
                ["us-sdkClientSettingsWebSocketInternal"] = new(),
                ["us-sdkClientStorm"] = new(),
                ["us-sdkClientUrls"] = new()
            }
        });
    }

    private static Parameter Group(params (string Name, object? Value)[] fields)
    {
        return Group(null, fields);
    }

    private static Parameter Group(
        RelatedPopulation? relatedPopulation,
        params (string Name, object? Value)[] fields)
    {
        return new Parameter
        {
            Fields = fields.ToDictionary(field => field.Name, field => field.Value),
            RelatedPopulation = relatedPopulation
        };
    }

    private static Parameter ObjectGroup(params string[] fieldNames)
    {
        return new Parameter
        {
            Fields = fieldNames.ToDictionary(fieldName => fieldName, _ => (object?)new { })
        };
    }
}
