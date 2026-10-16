using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("/v1/spaces/{spaceId:jdnext}/parameters")]
[Route("/v2/spaces/{spaceId:jdnext}/parameters")]
public class Parameters(IBundleService bundleService, ITagService tagService, IOptions<UrlSettings> urlSettings) : ControllerBase
{
    [HttpGet]
    public IActionResult GetParametersSpaces([FromRoute] Guid spaceId)
    {
        // Use the requesting host for the EFDS URL so both Switch (DNS redirect)
        // and phone (binary patched domain) reach the correct server
        string hostUrl = urlSettings.Value.HostUrl;
        string webSocketBaseUrl = $"wss://{hostUrl}";
        parameters.Parameters["ServerInfo"].Fields["url"] = $"https://{hostUrl}";
        parameters.Parameters["UbiConnect"].Fields["getVisualNotificationUrl"] =
            $"https://{hostUrl}/v1/profiles/{{profileId}}/global/ubiconnect/visualnotifications/api/{{visualNotificationType}}/{{visualNotificationId}}?spaceId={{spaceId}}";

        Dictionary<string, object?> sdkClientUrls = parameters.Parameters["us-sdkClientUrls"].Fields;
        sdkClientUrls["websocketServer"] = webSocketBaseUrl;
        sdkClientUrls["websocketNotifications"] = $"{webSocketBaseUrl}/v2/websocket";

        return Ok(parameters);
    }

    readonly ParameterList parameters = new()
    {
        Parameters = new()
        {
            ["us-GlobalSpaceConfig"] = new()
            {
                Fields = new()
                {
                    ["FriendsSpaceId"] = UbisoftServicesConstants.SocialSpaceId,
                    ["BlocklistSpaceId"] = UbisoftServicesConstants.SocialSpaceId
                }
            },
            ["us-sdkClientFeaturesSwitches"] = new()
            {
                Fields = new()
                {
                    ["ugc"] = true,
                    ["news"] = true,
                    ["tlog"] = true,
                    ["event"] = true,
                    ["mocks"] = false,
                    ["party"] = true,
                    ["stats"] = true,
                    ["token"] = true,
                    ["uplay"] = true,
                    ["users"] = true,
                    ["groups"] = true,
                    ["trades"] = true,
                    ["persona"] = true,
                    ["rewards"] = true,
                    ["calendar"] = true,
                    ["profiles"] = true,
                    ["blocklist"] = true,
                    ["challenge"] = true,
                    ["telemetry"] = true,
                    ["voicechat"] = true,
                    ["battlepass"] = true,
                    ["httpClient"] = true,
                    ["moderation"] = true,
                    ["parameters"] = true,
                    ["reputation"] = true,
                    ["clubService"] = true,
                    ["gamesPlayed"] = true,
                    ["matchmaking"] = true,
                    ["populations"] = true,
                    ["localization"] = true,
                    ["primaryStore"] = true,
                    ["remoteGaming"] = true,
                    ["richPresence"] = true,
                    ["createSession"] = true,
                    ["entitiesSpace"] = true,
                    ["extendSession"] = true,
                    ["friendsLookup"] = true,
                    ["leaderboardMe"] = true,
                    ["playerReports"] = true,
                    ["friendsRequest"] = true,
                    ["playerActivity"] = true,
                    ["playerConsents"] = true,
                    ["secondaryStore"] = true,
                    ["ubisoftConnect"] = true,
                    ["applicationUsed"] = true,
                    ["clubApplication"] = true,
                    ["entitiesProfile"] = true,
                    ["recommendations"] = true,
                    ["webSocketClient"] = true,
                    ["clubDynamicPanel"] = true,
                    ["notificationSend"] = true,
                    ["playerPrivileges"] = true,
                    ["usersLegalOptins"] = true,
                    ["eventsDefinitions"] = true,
                    ["groupsInvitations"] = true,
                    ["leaderboardSpaces"] = true,
                    ["playerPreferences"] = true,
                    ["playerOnlineStatus"] = true,
                    ["usersCreateAndLink"] = true,
                    ["friendsUplayProfile"] = true,
                    ["leaderboardProfiles"] = true,
                    ["friendsSpaceSpecific"] = true,
                    ["notificationSendBatch"] = true,
                    ["notificationWebsocket"] = true,
                    ["primaryStoreSendEvent"] = true,
                    ["applicationInformation"] = true,
                    ["notificationSendNoBroker"] = true,
                    ["notificationDynamicUpdate"] = true,
                    ["populationsAutomaticUpdate"] = true,
                    ["primaryStoreAutomaticFetch"] = true,
                    ["secondaryStoreTransactions"] = true,
                    ["secondaryStoreInstantiation"] = true,
                    ["secondaryStoreInventoryRules"] = true,
                    ["mobileExtensionUsersManagement"] = true,
                    ["notificationRequestConnections"] = true,
                    ["mobileExtensionProfilesExternal"] = true,
                    ["notificationRemoteLogReceivedData"] = false,
                    ["primaryStoreCatalogGetGameMediaType"] = false,
                    ["primarySecondaryStoreForceSyncAtLogin"] = true,
                    ["firstPartyAccessToMultiplayerInformation"] = true,
                    ["syncOnResumeToForegroundForNintendoSwitch"] = false,
                    ["avatars"] = true,
                    ["sanctions"] = true,
                    ["cloudSaves"] = true,
                    ["usersPolicies"] = true,
                    ["catalogDiscount"] = true,
                    ["trackingSession "] = false,
                    ["playerCodeOfConduct"] = true
                }
            },
            ["us-sdkClientMocks"] = new(),
            ["us-sdkClientNotificationsGame"] = new()
            {
                Fields = new()
                {
                    ["additionalSpaces"] = Array.Empty<object>(),
                    ["JD_CUSTOM_BATCH_TEST"] = true,
                    ["JD_CUSTOM_LEAVE_GROUP"] = true,
                    ["JD_CUSTOM_ONLINE_TEST"] = true,
                    ["JD_CUSTOM_GROUP_LEADER"] = true,
                    ["JD_CUSTOM_GROUP_SERVER"] = true,
                    ["JD_CUSTOM_KICKED_GROUP"] = true,
                    ["JD_CUSTOM_PARTY_ACCESS"] = true,
                    ["JD_CUSTOM_GROUP_METADATA"] = true,
                    ["JD_CUSTOM_GROUP_DANCER_CARD"] = true,
                    ["JD_CUSTOM_FRIEND_DANCER_CARD"] = true,
                    ["JD_CUSTOM_NOSTIFICATION_TEST"] = true,
                    ["JD_ONLINE_CUSTOM_NOTIFICATION"] = true,
                    ["JD_CUSTOM_GROUP_MEMBER_CHANGED"] = true,
                    ["UBICONNECT_VISUAL_NOTIFICATION"] = true,
                    ["JD_CUSTOM_FIRSTPARTY_INVITE_SENT"] = true
                }
            },
            ["us-sdkClientNotificationsInternal"] = new()
            {
                Fields = new()
                {
                    ["BLOCKLIST_ADD"] = true,
                    ["BLOCKLIST_ADDED"] = true,
                    ["BLOCKLIST_REMOVE"] = true,
                    ["BLOCKLIST_REMOVED"] = true,
                    ["PARTY_MEMBER_ADDED"] = true,
                    ["CLUB_BADGE_ACQUIRED"] = true,
                    ["GROUPS_MEMBER_ADDED"] = true,
                    ["PARTY_PARTY_CREATED"] = true,
                    ["PARTY_PARTY_DELETED"] = true,
                    ["PARTY_PARTY_UPDATED"] = true,
                    ["FRIENDS_INFO_UPDATED"] = true,
                    ["GROUPS_GROUP_DELETED"] = true,
                    ["GROUPS_GROUP_UPDATED"] = true,
                    ["PARTY_MEMBER_REMOVED"] = true,
                    ["PARTY_MEMBER_UPDATED"] = true,
                    ["SSI_INSTANCES_UPDATE"] = true,
                    ["CLUB_ACTION_COMPLETED"] = true,
                    ["CLUB_CHALLENGE_BANKED"] = true,
                    ["CLUB_REWARD_PURCHASED"] = true,
                    ["GROUPS_MEMBER_REMOVED"] = true,
                    ["GROUPS_MEMBER_UPDATED"] = true,
                    ["PARTY_INVITATION_SENT"] = true,
                    ["PARTY_JOINREQUEST_SENT"] = true,
                    ["BATTLEPASS_TIERS_BANKED"] = true,
                    ["CLUB_CHALLENGE_COMPLETED"] = true,
                    ["PARTY_INVITATION_EXPIRED"] = true,
                    ["CHALLENGES_REWARDS_BANKED"] = true,
                    ["PARTY_INVITATION_ACCEPTED"] = true,
                    ["PARTY_INVITATION_DECLINED"] = true,
                    ["PARTY_JOINREQUEST_EXPIRED"] = true,
                    ["US_APP_PARAMS_FULL_UPDATE"] = true,
                    ["GROUPS_GROUP_OWNER_UPDATED"] = true,
                    ["PARTY_INVITATION_CANCELLED"] = true,
                    ["PARTY_JOINREQUEST_ACCEPTED"] = true,
                    ["PARTY_JOINREQUEST_DECLINED"] = true,
                    ["PARTY_PARTY_EXPIRY_RENEWED"] = true,
                    ["US_APP_PARAMS_GROUP_UPDATE"] = true,
                    ["FRIENDS_RELATIONSHIP_UPDATE"] = true,
                    ["PARTY_JOINREQUEST_CANCELLED"] = true,
                    ["US_NOTIFICATION_MAINTENANCE"] = true,
                    ["US_SPACE_PARAMS_FULL_UPDATE"] = true,
                    ["CLUB_CHALLENGE_PARTICIPATION"] = true,
                    ["FRIENDS_RELATIONSHIP_TRIGGER"] = true,
                    ["US_SPACE_PARAMS_GROUP_UPDATE"] = true,
                    ["PARTY_PARTY_LOCKSTATE_UPDATED"] = true,
                    ["US_CONCURRENT_CONNECT_DETECTED"] = true,
                    ["GROUPSINVITATIONS_GROUP_UPDATED"] = true,
                    ["CHALLENGES_PROGRESSION_COMPLETED"] = true,
                    ["CLUB_CHALLENGE_THRESHOLD_REACHED"] = true,
                    ["US_CLIENT_SECONDARY_STORE_UPDATE"] = true,
                    ["GROUPSINVITATIONS_INVITATION_SENT"] = true,
                    ["PARTY_INVITATION_WORKFLOW_STARTED"] = true,
                    ["GROUPSINVITATIONS_JOINREQUEST_SENT"] = true,
                    ["GROUPSINVITATIONS_GROUP_LOCK_UPDATED"] = true,
                    ["GROUPSINVITATIONS_INVITATION_EXPIRED"] = true,
                    ["GROUPSINVITATIONS_INVITATION_ACCEPTED"] = true,
                    ["GROUPSINVITATIONS_INVITATION_DECLINED"] = true,
                    ["GROUPSINVITATIONS_JOINREQUEST_EXPIRED"] = true,
                    ["GROUPSINVITATIONS_INVITATION_CANCELLED"] = true,
                    ["GROUPSINVITATIONS_JOINREQUEST_ACCEPTED"] = true,
                    ["GROUPSINVITATIONS_JOINREQUEST_DECLINED"] = true,
                    ["GROUPS_POLICY_RETENTION_EXPIRY_RENEWED"] = true,
                    ["PARTY_JOINREQUEST_PLAYER_PREAUTHORIZED"] = true,
                    ["GROUPSINVITATIONS_JOINREQUEST_CANCELLED"] = true,
                    ["TRADE_UPDATED"] = true,
                    ["MM_ASK_GROUP_METADATA"] = true,
                    ["FRIENDS_STATUS_CHANGED"] = true,
                    ["PARTY_PARTY_MOVE_FAILED"] = true,
                    ["PERSONA_PERSONA_CREATED"] = true,
                    ["PERSONA_PERSONA_DELETED"] = true,
                    ["PERSONA_PERSONA_UPDATED"] = true,
                    ["BATTLEPASS_TIERS_REACHED"] = true,
                    ["PARTY_INVITATION_DELETED"] = true,
                    ["PRODUCTS_CATALOG_UPDATED"] = false,
                    ["PARTY_PARTY_OWNER_UPDATED"] = true,
                    ["UBICONNECT_REWARD_PURCHASED"] = false,
                    ["PARTY_ACCESSREQUEST_ACCEPTED"] = true,
                    ["PLAYERACTIVITY_ACTIVITIES_CLOSED"] = true,
                    ["PLAYERACTIVITY_ACTIVITIES_CREATED"] = true,
                    ["PLAYERACTIVITY_ACTIVITIES_UPDATED"] = true,
                    ["PLAYERPRIVILEGES_PRIVILEGES_UPDATED"] = true,
                    ["PARTY_LIMITS_MAXIMUM_MEMBERS_UPDATED"] = true,
                    ["REPUTATION_PLAYERFACINGLEVEL_UPDATED"] = true,
                    ["PARTY_PARTY_INVITATIONSCONFIG_UPDATED"] = true,
                    ["PARTY_PARTY_JOINREQUESTSCONFIG_UPDATED"] = true,
                    ["RICHPRESENCE_PLAYER_RICHPRESENCE_CHANGED"] = true
                }
            },
            ["us-sdkClientNotificationsSpaceIds"] = new()
            {
                Fields = new()
                {
                    ["FriendsService"] = JdNextConstants.FriendsServiceId,
                    ["BlocklistService"] = UbisoftServicesConstants.SocialSpaceId,
                    ["PlayerPrivilegesService"] = JdNextConstants.PlayerPrivilegesServiceId
                }
            },
            ["us-sdkClientRemoteLogsGame"] = new()
            {
                Fields = new()
                {
                    ["url"] = "",
                    ["maxTextLength"] = 32768,
                    ["uncategorized"] = "None",
                    ["ActivityPageService"] = "Error"
                }
            },
            ["us-sdkClientRemoteLogsInternal"] = new()
            {
                Fields = new()
                {
                    ["job"] = "None",
                    ["ugc"] = "Warning",
                    ["club"] = "None",
                    ["core"] = "None",
                    ["http"] = "None",
                    ["news"] = "None",
                    ["tLog"] = "Warning",
                    ["task"] = "None",
                    ["test"] = "None",
                    ["user"] = "None",
                    ["async"] = "None",
                    ["event"] = "None",
                    ["mocks"] = "None",
                    ["party"] = "None",
                    ["stats"] = "None",
                    ["token"] = "None",
                    ["uplay"] = "None",
                    ["entity"] = "None",
                    ["friend"] = "None",
                    ["groups"] = "Warning",
                    ["trades"] = "None",
                    ["overlay"] = "Warning",
                    ["persona"] = "None",
                    ["profile"] = "None",
                    ["calendar"] = "Warning",
                    ["blocklist"] = "None",
                    ["challenge"] = "None",
                    ["remoteLog"] = "None",
                    ["sanctions"] = "None",
                    ["scheduler"] = "None",
                    ["telemetry"] = "None",
                    ["voicechat"] = "None",
                    ["websocket"] = "None",
                    ["battlepass"] = "Warning",
                    ["connection"] = "None",
                    ["httpEngine"] = "None",
                    ["moderation"] = "Warning",
                    ["parameters"] = "Warning",
                    ["population"] = "None",
                    ["reputation"] = "None",
                    ["gamesPlayed"] = "None",
                    ["leaderboard"] = "None",
                    ["matchmaking"] = "None",
                    ["userContent"] = "None",
                    ["localization"] = "None",
                    ["notification"] = "None",
                    ["primaryStore"] = "None",
                    ["remoteGaming"] = "Warning",
                    ["richPresence"] = "None",
                    ["configuration"] = "None",
                    ["maxTextLength"] = 32768,
                    ["playerReports"] = "None",
                    ["usersPolicies"] = "None",
                    ["authentication"] = "None",
                    ["playerActivity"] = "None",
                    ["playerConsents"] = "None",
                    ["secondaryStore"] = "None",
                    ["applicationUsed"] = "None",
                    ["catalogDiscount"] = "Warning",
                    ["mobileExtension"] = "None",
                    ["recommendations"] = "None",
                    ["coreNotification"] = "None",
                    ["playerPrivileges"] = "None",
                    ["groupsInvitations"] = "Warning",
                    ["playerPreferences"] = "None",
                    ["playerOnlineStatus"] = "None",
                    ["applicationInformation"] = "None",
                    ["secondaryStoreInstantiation"] = "None",
                    ["secondaryStoreInventoryRules"] = "None",
                    ["avatars"] = "warning",
                    ["cloudSaves"] = "None",
                    ["ubisoftConnect"] = "None"
                }
            },
            ["us-sdkClientSettings"] = new()
            {
                Fields = new()
                {
                    ["popEventsTimeoutMsec"] = 5000,
                    ["primaryStoreSyncDelayMsec"] = 250,
                    ["createSessionRestPeriodMSec"] = 3000,
                    ["createSessionRestRandomMSec"] = 5000,
                    ["partyClientXblComplianceMethod"] = "mpsd",
                    ["secondaryStoreInventoryRulesMode"] = "Non-blocking",
                    ["xboxOneResumeFromSuspendedDelayMsec"] = 5000,
                    ["enableCrossPlayPreferenceSyncForXbox"] = false,
                    ["enablePartyClientFirstPartyCompliance"] = true,
                    ["xboxOneCloseWebsocketOnSuspendingMode"] = "All",
                    ["notificationTypesUpdateRandomDelayMsec"] = 3000,
                    ["waitRemoteLogCompletionOnDeleteSession"] = false,
                    ["secondaryStoreInventoryRulesRetryDelayMsec"] = 5000,
                    ["minBackgroundDurationForInventoryInvalidationMsec"] = 5000,
                    ["createSessionRestPeriodAfterConcurrentConnectDetectedMSec"] = 60000
                }
            },
            ["us-sdkClientSettingsCacheTTL"] = new()
            {
                Fields = new()
                {
                    ["defaultSec"] = 120,
                    ["spacesNewsSec"] = 900,
                    ["friendsListSec"] = 1800,
                    ["ssiRulesAllSec"] = 86400,
                    ["profilesNewsSec"] = 900,
                    ["profilesActionsSec"] = 120,
                    ["profilesRewardsSec"] = 120,
                    ["ssiAttributesAllSec"] = 86400,
                    ["profilesChallengesSec"] = 120,
                    ["ssiListsOfAttributesAllSec"] = 86400,
                    ["challengesDefinitionSeasonSec"] = 180,
                    ["battlepassSeasonProgressionSec"] = 60,
                    ["profilesChallengesStatusSeasonSec"] = 60,
                    ["friendsConfigSec"] = 1800,
                    ["spacesParametersSec"] = 120,
                    ["partiesJoinAndInviteSec"] = 120,
                    ["applicationsParametersSec"] = 120
                }
            },
            ["us-sdkClientSettingsHttpGame"] = new()
            {
                Fields = new()
                {
                    ["maxCount"] = 2,
                    ["retryMaxDelayMsec"] = 5000,
                    ["retryRandomDelayMsec"] = 5000,
                    ["retryInitialDelayMsec"] = 5000,
                    ["timeoutInitialDelayMsec"] = 25000,
                    ["retryIncrementFactorMsec"] = 2500,
                    ["timeoutIncrementFactorMsec"] = 2500
                }
            },
            ["us-sdkClientSettingsHttpInternal"] = new()
            {
                Fields = new()
                {
                    ["maxCount"] = 2,
                    ["retryMaxDelayMsec"] = 3600000,
                    ["retryRandomDelayMsec"] = 5000,
                    ["retryInitialDelayMsec"] = 5000,
                    ["timeoutInitialDelayMsec"] = 25000,
                    ["retryIncrementFactorMsec"] = 2500,
                    ["timeoutIncrementFactorMsec"] = 2500
                }
            },
            ["us-sdkClientSettingsSecondaryStoreSync"] = new()
            {
                Fields = new()
                {
                    ["maxCount"] = 10,
                    ["restPeriodMsec"] = 30000,
                    ["retryMaxDelayMsec"] = 3600000,
                    ["retryRandomDelayMsec"] = 5000,
                    ["retryInitialDelayMsec"] = 5000,
                    ["retryIncrementFactorMsec"] = 5000,
                    ["subscriptionAutomaticSyncPeriodInHoursGDK"] = 0
                }
            },
            ["us-sdkClientSettingsWebSocketGame"] = new()
            {
                Fields = new()
                {
                    ["maxCount"] = 50,
                    ["retryMaxDelayMsec"] = 5000,
                    ["retryRandomDelayMsec"] = 5000,
                    ["retryInitialDelayMsec"] = 5000,
                    ["timeoutInitialDelayMsec"] = 30000,
                    ["retryIncrementFactorMsec"] = 5000,
                    ["connectionPingIntervalSec"] = 30,
                    ["timeoutIncrementFactorMsec"] = 5000
                }
            },
            ["us-sdkClientSettingsWebSocketInternal"] = new()
            {
                Fields = new()
                {
                    ["maxCount"] = 50,
                    ["retryMaxDelayMsec"] = 900000,
                    ["retryRandomDelayMsec"] = 5000,
                    ["retryInitialDelayMsec"] = 0,
                    ["timeoutInitialDelayMsec"] = 30000,
                    ["retryIncrementFactorMsec"] = 1500,
                    ["connectionPingIntervalSec"] = 30,
                    ["timeoutIncrementFactorMsec"] = 5000
                }
            },
            ["us-sdkClientStorm"] = new()
            {
                Fields = []
            },
            ["us-sdkClientUrls"] = new()
            {
                Fields = new()
                {
                    ["news"] = "{baseurl_aws}/{version}/profiles/me/news",
                    ["tLog"] = "https://tglog.datamore.qq.com/{appId}/report/",
                    ["users"] = "{baseurl_aws}/{version}/users",
                    ["events"] = "{baseurl_aws}/{version}/profiles/{profileId}/events",
                    ["groups"] = "{baseurl_aws}/{version}/groups",
                    ["friends"] = "{baseurl_aws}/{version}/profiles/me/friends",
                    ["calendar"] = "{baseurl_aws}/{version}/spaces/{spaceId}/calendarentries",
                    ["policies"] = "{baseurl_aws}/{version}/policies",
                    ["profiles"] = "{baseurl_aws}/{version}/profiles",
                    ["sessions"] = "{baseurl_aws}/{version}/profiles/sessions",
                    ["blocklist"] = "{baseurl_aws}/{version}/profiles/{profileId}/blocks",
                    ["challenge"] = "{baseurl_aws}/{version}/spaces/{spaceId}/challenges",
                    ["groupType"] = "{baseurl_aws}/{version}/spaces/{spaceId}/grouptypes/{groupTypeId}",
                    ["sandboxes"] = "{baseurl_aws}/{version}/spaces/{spaceId}/sandboxes",
                    ["telemetry"] = "{baseurl_aws}/{version}/spaces/{spaceId}/global/tgdp/telemetry/gateway/data",
                    ["moderation"] = "{baseurl_aws}/{version}/spaces/{spaceId}/moderation/{text}",
                    ["remoteLogs"] = "{baseurl_aws}/{version}/profiles/me/remotelog",
                    ["spacesNews"] = "{baseurl_aws}/{version}/spaces/news",
                    ["tokenSpace"] = "{baseurl_aws}/{version}/spaces/{spaceId}/tokens",
                    ["connections"] = "{baseurl_aws}/{version}/profiles/{profileId}/connections",
                    ["gamesPlayed"] = "{baseurl_aws}/{version}/profiles/{profileId}/gamesplayed",
                    ["spacesItems"] = "{baseurl_aws}/{version}/spaces/{spaceId}/items",
                    ["spacesStats"] = "{baseurl_aws}/{version}/spaces/{spaceId}/communitystats",
                    ["applications"] = "{baseurl_aws}/{version}/applications/{applicationId}/configuration",
                    ["localization"] = "{baseurl_aws}/{version}/spaces/{spaceId}/localizations/strings",
                    ["personaSpace"] = "{baseurl_aws}/{version}/profiles/persona",
                    ["spacesOffers"] = "{baseurl_aws}/{version}/spaces/{spaceId}/offers",
                    ["tokenProfile"] = "{baseurl_aws}/{version}/profiles/{profileId}/tokens",
                    ["allgroupTypes"] = "{baseurl_aws}/{version}/spaces/{spaceId}/grouptypes",
                    ["calendarLists"] = "{baseurl_aws}/{version}/spaces/{spaceId}/calendarentrylists",
                    ["configsEvents"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/events",
                    ["groupsMembers"] = "{baseurl_aws}/{version}/groups/{groupId}/members",
                    ["partyXboxSync"] = "{baseurl_aws}/{version}/spaces/{spaceId}/parties/{partyId}/firstPartyParameters/xblMultiplayerSessionReferenceUri",
                    ["profilesStats"] = "{baseurl_aws}/{version}/profiles/{profileId}/stats",
                    ["profilesToken"] = "{baseurl_aws}/{version}/profiles/{profileId}/tokens/{token}",
                    ["spacesActions"] = "{baseurl_aws}/{version}/spaces/{spaceId}/actions",
                    ["spacesParties"] = "{baseurl_aws}/{version}/spaces/{spaceId}/parties",
                    ["spacesRewards"] = "{baseurl_aws}/{version}/spaces/{spaceId}/rewards",
                    ["allConnections"] = "{baseurl_aws}/{version}/profiles/connections",
                    ["allSpacesItems"] = "{baseurl_aws}/{version}/spaces/items",
                    ["moderationPOST"] = "{baseurl_aws}/{version}/spaces/{spaceId}/moderation",
                    ["personaProfile"] = "{baseurl_aws}/{version}/profiles/{profileId}/persona",
                    ["playerConsents"] = "{baseurl_aws}/{version}/profiles/{profileId}/consents/categories",
                    ["profilesGroups"] = "{baseurl_aws}/{version}/profiles/{profileId}/groups",
                    ["spacesEntities"] = "{baseurl_aws}/{version}/spaces/{spaceId}/entities",
                    ["allSpacesOffers"] = "{baseurl_aws}/{version}/spaces/offers",
                    ["localizationAll"] = "{baseurl_aws}/{version}/spaces/{spaceId}/localizations/strings/all",
                    ["profilesActions"] = "{baseurl_aws}/{version}/profiles/{profileId}/club/actions",
                    ["profilesFriends"] = "{baseurl_aws}/{version}/profiles/{profileId}/friends",
                    ["profilesParties"] = "{baseurl_aws}/{version}/profiles/{profileId}/parties",
                    ["profilesRewards"] = "{baseurl_aws}/{version}/profiles/{profileId}/club/rewards",
                    ["recommendations"] = "{baseurl_aws}/{version}/profiles/{profileId}/recommendations",
                    ["spacesStatsCard"] = "{baseurl_aws}/{version}/spaces/{spaceId}/communitystatscard",
                    ["websocketServer"] = "{baseurl_ws}",
                    ["allProfilesStats"] = "{baseurl_aws}/{version}/profiles/stats",
                    ["blocklistUnblock"] = "{baseurl_aws}/{version}/profiles/{profileId}/blocks/{blockedProfileId}",
                    ["profilesEntities"] = "{baseurl_aws}/{version}/profiles/{profileId}/entities",
                    ["profilesExternal"] = "{baseurl_aws}/{version}/profiles/external",
                    ["profilesMeEvents"] = "{baseurl_aws}/{version}/profiles/me/events",
                    ["profilesUgcViews"] = "{baseurl_aws}/{version}/profiles/ugc/{contentId}/views",
                    ["spacesChallenges"] = "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty",
                    ["spacesConfigsUgc"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/ugc",
                    ["spacesParameters"] = "{baseurl_aws}/{version}/spaces/{spaceId}/parameters",
                    ["tradesItemsGifts"] = "{baseurl_aws}/{version}/profiles/{profileId}/trades/itemsgifts",
                    ["tradesOfferGifts"] = "{baseurl_aws}/{version}/profiles/{profileId}/trades/offergifts",
                    ["allSpacesEntities"] = "{baseurl_aws}/{version}/spaces/entities",
                    ["eventsDefinitions"] = "{baseurl_aws}/{version}/spaces/{spaceId}/eventsDefinitions",
                    ["profilesInventory"] = "{baseurl_aws}/{version}/profiles/{profileId}/inventory",
                    ["profilesStatsCard"] = "{baseurl_aws}/{version}/profiles/statscard",
                    ["profilesUgcPhotos"] = "{baseurl_aws}/{version}/profiles/ugc/photos",
                    ["spacesLeaderboard"] = "{baseurl_aws}/{version}/spaces/{spaceId}/leaderboards",
                    ["blocklistBlockedBy"] = "{baseurl_aws}/{version}/profiles/{profileId}/blocks/blockedBy",
                    ["profilesChallenges"] = "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty",
                    ["profilesReputation"] = "{baseurl_aws}/{version}/profiles/{profileId}/reputation",
                    ["profilesUgcRatings"] = "{baseurl_aws}/{version}/profiles/{profileId}/ugc/ratings",
                    ["spacesBattlepasses"] = "{baseurl_aws}/{version}/spaces/{spaceId}/battlepasses",
                    ["allProfilesEntities"] = "{baseurl_aws}/{version}/profiles/entities",
                    ["gamesPlayedProfiles"] = "{baseurl_aws}/{version}/profiles/gamesplayed",
                    ["profilesLeaderboard"] = "{baseurl_aws}/{version}/profiles/ranks",
                    ["spacesRichPresences"] = "{baseurl_aws}/{version}/spaces/{spaceId}/richpresences",
                    ["usersOnlineStatuses"] = "{baseurl_aws}/{version}/users/onlineStatuses",
                    ["voicechatTokenVivox"] = "{baseurl_aws}/{version}/profiles/{profileId}/voicechattoken/vivox",
                    ["applicationsMetadata"] = "{baseurl_aws}/{version}/applications",
                    ["challengeProgression"] = "{baseurl_aws}/{version}/profiles/{profileId}/challenges/progressions",
                    ["playerReportsProfile"] = "{baseurl_aws}/{version}/profiles/{profileId}/reports",
                    ["profilesApplications"] = "{baseurl_aws}/{version}/profiles/me/applications",
                    ["profilesUgcFavorites"] = "{baseurl_aws}/{version}/profiles/{profileId}/ugc/favorites",
                    ["profilesUgcPhotosOwn"] = "{baseurl_aws}/{version}/profiles/{profileId}/ugc/photos",
                    ["spacesChallengepools"] = "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty",
                    ["voicechatConfigVivox"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/voicechat/vivox",
                    ["groupsRetentionExpiry"] = "{baseurl_aws}/{version}/groups/{groupId}/policies/retention/expiry",
                    ["profilesMeLeaderboard"] = "{baseurl_aws}/{version}/profiles/me/ranks",
                    ["profilesNotifications"] = "{baseurl_aws}/{version}/profiles/{profileId}/notifications",
                    ["spacesConfigsSsiRules"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/secondarystore/instances/rules",
                    ["usersMeOnlineStatuses"] = "{baseurl_aws}/{version}/users/me/onlineStatuses",
                    ["applicationsParameters"] = "{baseurl_aws}/{version}/applications/{applicationId}/parameters",
                    ["playerConsentsCategory"] = "{baseurl_aws}/{version}/profiles/{profileId}/consents/categories/{categoryName}",
                    ["spacesSeasonChallenges"] = "{baseurl_aws}/{version}/spaces/{spaceId}/club/seasonchallenges",
                    ["tradesItemsGiftsConfig"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/trades/itemsgifts",
                    ["tradesOfferGiftsConfig"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/trades/offergifts",
                    ["websocketNotifications"] = "{baseurl_ws}/{version}/websocket",
                    ["allProfilesApplications"] = "{baseurl_aws}/{version}/profiles/applications",
                    ["groupsInvitationsConfig"] = "{baseurl_aws}/{version}/groups/{groupId}/configs/groupsinvitations",
                    ["playerPrivilegesProfile"] = "{baseurl_aws}/{version}/profiles/{profileId}/playerprivileges",
                    ["profilesUgcUpdateRating"] = "{baseurl_aws}/{version}/profiles/{profileId}/ugc/{contentId}/ratings",
                    ["groupsInvitationsInvites"] = "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/invitations",
                    ["profilesSeasonChallenges"] = "{baseurl_aws}/{version}/profiles/{profileId}/club/seasonchallenges?spaceId={spaceId}",
                    ["profilesUgcReportContent"] = "{baseurl_aws}/{version}/profiles/{profileId}/ugc/{contentId}/reports",
                    ["groupsInvitationsProfiles"] = "{baseurl_aws}/{version}/profiles/{profileId}/groupsInvitations",
                    ["profilesMeRoamingProfiles"] = "{baseurl_aws}/{version}/users/me/roamingProfiles",
                    ["profilesProfileChallenges"] = "{baseurl_aws}/v2/spaces/global/ubiconnect/challenges/api/legacy/empty",
                    ["profilesUgcExternalVideos"] = "{baseurl_aws}/{version}/profiles/ugc/externalvideos",
                    ["profilesUgcUpdateFavorite"] = "{baseurl_aws}/{version}/profiles/{profileId}/ugc/{contentId}/favorites",
                    ["spacesBattlepassesSeasons"] = "{baseurl_aws}/{version}/spaces/{spaceId}/battlepasses/seasons",
                    ["spacesCommunityChallenges"] = "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty",
                    ["spacesConfigsPrimarystore"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/primarystore",
                    ["groupsInvitationsLockState"] = "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/lockstate",
                    ["profilesInventoryInstances"] = "{baseurl_aws}/{version}/profiles/{profileId}/inventory/instances",
                    ["profilesNotificationsBatch"] = "{baseurl_aws}/{version}/profiles/notifications",
                    ["spacesConfigsSsiAttributes"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/secondarystore/instances/attributes",
                    ["profilesBattlepassesSeasons"] = "{baseurl_aws}/{version}/profiles/{profileId}/battlepasses/seasons",
                    ["playerReportsSpaceCategories"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/reports/categories",
                    ["groupsInvitationsJoinRequests"] = "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/joinrequests",
                    ["groupsInvitationsUpdateInvite"] = "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/invitations/{inviteeProfileId}/state",
                    ["profilesInventoryPrimarystore"] = "{baseurl_aws}/{version}/profiles/{profileId}/inventory/primarystore",
                    ["profilesInventoryTransactions"] = "{baseurl_aws}/{version}/profiles/{profileId}/inventory/transactions",
                    ["profilesMeBattlepassesSeasons"] = "{baseurl_aws}/{version}/profiles/me/battlepasses/seasons",
                    ["profilesMeCommunityChallenges"] = "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty",
                    ["matchmakingGroupsMatchesPrecise"] = "{baseurl_aws}/{version}/groups/{groupId}/matches/precise",
                    ["profilesInventoryExpiredDetails"] = "{baseurl_aws}/{version}/profiles/{profileId}/inventory/expiredDetails",
                    ["profilesMeInventoryPrimarystore"] = "{baseurl_aws}/{version}/profiles/me/inventory/primarystore",
                    ["profilesPreciseMatchmakingMatch"] = "{baseurl_aws}/{version}/profiles/{profileId}/matches/precise/matchstate",
                    ["spacesPlayerPreferencesStandard"] = "{baseurl_aws}/{version}/spaces/{spaceId}/standardpreferences",
                    ["profilesBattlepassesSeasonsTiers"] = "{baseurl_aws}/{version}/profiles/{profileId}/battlepasses/seasons/{seasonId}/tiers",
                    ["profilesPreciseMatchmakingClient"] = "{baseurl_aws}/{version}/profiles/{profileId}/matches/precise/clientstate",
                    ["profilesPlayerPreferencesStandard"] = "{baseurl_aws}/{version}/profiles/{profileId}/standardpreferences",
                    ["profilesUgcRequestReportedContent"] = "{baseurl_aws}/{version}/profiles/{profileId}/ugc/reports",
                    ["spacesBattlepassesSeasonsSeasonId"] = "{baseurl_aws}/{version}/spaces/{spaceId}/battlepasses/seasons/{seasonId}",
                    ["spacesConfigsSsiListsOfAttributes"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/secondarystore/instances/listsOfAttributes",
                    ["usersMeOnlineStatusesManualStatus"] = "{baseurl_aws}/{version}/users/me/onlineStatuses/manualStatus",
                    ["groupsInvitationsUpdateJoinRequest"] = "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/joinrequests/{requesterProfileId}/state",
                    ["matchmakingSpaceGlobalHarboursocial"] = "{baseurl_aws}/{version}/spaces/{spaceId}/global/harboursocial/matchmaking",
                    ["spacesPartiesPartyIdMembersProfileId"] = "{baseurl_aws}/{version}/spaces/{spaceId}/parties/{partyId}/members/{profileId}",
                    ["profilesMeBattlepassesSeasonsSeasonId"] = "{baseurl_aws}/{version}/profiles/me/battlepasses/seasons/{seasonId}",
                    ["secondaryStoreInventoryRulesExecution"] = "{baseurl_aws}/{version}/profiles/{profileId}/inventory/rulesexecution",
                    ["matchmakingProfilesGlobalHarboursocial"] = "{baseurl_aws}/{version}/profiles/{profileId}/global/harboursocial/matchmaking",
                    ["profilesInventoryInstancesTransactions"] = "{baseurl_aws}/{version}/profiles/{profileId}/inventory/instances/transactions",
                    ["avatars"] = "{baseurl_aws}/{version}/profiles/{profileId}/avatars",
                    ["usersPolicies"] = "{baseurl_aws}/{version}/users/{userId}/policies",
                    ["friendsConfigs"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/friends",
                    ["trackingSession"] = "{baseurl_aws}/{version}/profiles/{profileId}/trackingSessions",
                    ["groupsMatchmaking"] = "{baseurl_aws}/{version}/groups/{groupId}/matchmaking",
                    ["groupsRichPresences"] = "{baseurl_aws}/{version}/groups/{groupId}/richpresences",
                    ["spacesPlayerActivity"] = "{baseurl_aws}/{version}/spaces/{spaceId}/playeractivities/activities",
                    ["profilesRichPresences"] = "{baseurl_aws}/{version}/profiles/{profileId}/richpresences",
                    ["challengeManualBanking"] = "{baseurl_aws}/{version}/profiles/{profileId}/challenges/{challengeId}",
                    ["playerActivityProfiles"] = "{baseurl_aws}/{version}/profiles/{profileId}/playeractivities/activities",
                    ["ubiConnectRewardsSpace"] = "{baseurl_aws}/{version}/spaces/{spaceId}/global/ubiconnect/rewards/api",
                    ["voicechatConfigPlayfab"] = "{baseurl_aws}/{version}/spaces/{spaceId}/configs/voicechat/playfab",
                    ["profilesOffersDiscounts"] = "{baseurl_aws}/{version}/profiles/{profileId}/offersdiscounts",
                    ["voicechatNetworkPlayfab"] = "{baseurl_aws}/{version}/spaces/{spaceId}/voicechatnetworks/playfab",
                    ["groupsMatchmakingMatches"] = "{baseurl_aws}/{version}/groups/{groupId}/matches",
                    ["groupsUgcGenericContents"] = "{baseurl_aws}/{version}/groups/ugc/genericcontents",
                    ["playerConsentsNextConfig"] = "{baseurl_aws}/{version}/spaces/configs/consents",
                    ["ubiConnectRewardsProfile"] = "{baseurl_aws}/{version}/profiles/{profileId}/global/ubiconnect/rewards/api",
                    ["playerConsentsNextProfile"] = "{baseurl_aws}/{version}/profiles/{profileId}/consents",
                    ["profilesInventoryReserves"] = "{baseurl_aws}/{version}/profiles/{profileId}/items/reserves",
                    ["sanctionsAppliedSanctions"] = "{baseurl_aws}/{version}/profiles/{profileId}/sanctions",
                    ["profilesMatchmakingMatches"] = "{baseurl_aws}/{version}/profiles/{profileId}/matches",
                    ["profilesUgcGenericContents"] = "{baseurl_aws}/{version}/profiles/ugc/genericcontents",
                    ["tradesSpaceMarketableItems"] = "{baseurl_aws}/{version}/spaces/{spaceId}/marketableitems",
                    ["groupsUgcGenericContentsOwn"] = "{baseurl_aws}/{version}/groups/{groupId}/ugc/genericcontents",
                    ["playerConsentsNextAcceptances"] = "{baseurl_aws}/{version}/profiles/{profileId}/consents/acceptances",
                    ["profilesUgcGenericContentsOwn"] = "{baseurl_aws}/{version}/profiles/{profileId}/ugc/genericcontents",
                    ["tradesProfilesMarketableItems"] = "{baseurl_aws}/{version}/profiles/{profileId}/marketableitems",
                    ["profilesOffersDiscountsMatches"] = "{baseurl_aws}/{version}/profiles/{profileId}/offersdiscountsmatches",
                    ["profilesMatchmakingOnlineAccess"] = "{baseurl_aws}/{version}/profiles/{profileId}/onlineAccess",
                    ["cloudSavesProfilesCloudSaveFiles"] = "{baseurl_aws}/{version}/profiles/{profileId}/cloudsavefiles",
                    ["profilesOffersDiscountsResolutions"] = "{baseurl_aws}/{version}/profiles/{profileId}/offersdiscountsresolutions",
                    ["ubiConnectCommunityChallengesSpace"] = "{baseurl_aws}/{version}/spaces/{spaceId}/global/ubiconnect/challenges/api/community",
                    ["ubiConnectCommunityChallengesProfile"] = "{baseurl_aws}/{version}/profiles/{profileId}/global/ubiconnect/challenges/api/community",
                    ["ubiConnectTimeLimitedChallengesSpace"] = "{baseurl_aws}/{version}/spaces/{spaceId}/global/ubiconnect/challenges/api/timelimited",
                    ["ubiConnectTimeLimitedChallengesProfile"] = "{baseurl_aws}/{version}/profiles/{profileId}/global/ubiconnect/challenges/api/timelimited",
                    ["ubiConnectApplicableTimeLimitedChallengesProfiles"] = "{baseurl_aws}/{version}/profiles/global/ubiconnect/challenges/api/timelimited/applicable"
                }
            },
            ["us-simulation"] = new()
            {
                Fields = new()
                {
                    ["timeoffsetminutes"] = 0
                }
            },
            ["ActivityPage"] = new()
            {
                Fields = new()
                {
                    ["Template"] = new Template()
                }
            },
            ["customClientFeaturesSwitches"] = new()
            {
                Fields = new()
                {
                    ["BossMode"] = true,
                    ["PartyMode"] = true,
                    ["CameraScoring"] = true,
                    ["PairingQRCode"] = true,
                    ["CameraScoringSurveyPopup"] = false,
                    ["DecadeMode"] = true
                }
            },
            ["GameplaySettings"] = new()
            {
                Fields = new()
                {
                    ["ClaimDisplayPriority"] = bundleService.ClaimDisplayPriority,
                    ["MCA_DeviceInclusionList"] = new Dictionary<string, object>(),
                    ["CameraScoring_SurveyPopupSettings"] = new Dictionary<string, object>(),
                    ["CameraScoring_MoveScoreBoostPercent"] = "0",
                    ["CameraScoring_MoveEvaluationTimeOffsetMs"] = "0",
                    ["CameraScoring_MoveScoreBoostPercent_SCA_iOS"] = "0",
                    ["CameraScoring_MoveEndToFeedbackDisplayDelayMs"] = "0",
                    ["CameraScoring_NoMoveMalusEnergyAmountThreshold"] = "0",
                    ["CameraScoring_CharityBonusEnergyFactorThreshold"] = "0",
                    ["CameraScoring_MoveScoreBoostPercent_SCA_Android"] = "0",
                    ["CameraScoring_MoveEvaluationTimeOffsetMs_SCA_iOS"] = "0",
                    ["CameraScoring_MoveEvaluationTimeOffsetMs_SCA_Android"] = "0",
                    ["DecadeModeRequiredPacks"] = new[] { "songpack_year5", "ANNUAL_EDITION_YEAR5_JD27" },
                    ["DecadeModeSongsList"] = new Dictionary<string, string>
                    {
                        ["IntroSong"] = "39f33429-8aff-441c-98d4-fb225e04ee3f",
                        ["DecadePointOfInterestSongConfig_00s_Song_01"] = "fe54a039-c58d-415c-b798-755b1bce47a6",
                        ["DecadePointOfInterestSongConfig_00s_Song_02"] = "eeeb3caa-5d71-40d1-8e7c-88fc12c1b5ab",
                        ["DecadePointOfInterestSongConfig_00s_Song_03"] = "7e150341-7674-428d-a962-7c727ce90b84",
                        ["DecadePointOfInterestSongConfig_00s_Song_04"] = "2a95dfbb-63e6-4c2d-a6d3-9a1fc688c0b0",
                        ["DecadePointOfInterestSongConfig_00s_Song_05"] = "63fbd5d9-bec0-4af4-8a6a-7cb87c580fda",
                        ["DecadePointOfInterestSongConfig_00s_Song_06"] = "9ff1a7db-10c3-491b-bdb8-64cafcc16f76",
                        ["DecadePointOfInterestSongConfig_10s_Song_01"] = "c0943ff2-0382-482d-bed7-1f0cb1d3896b",
                        ["DecadePointOfInterestSongConfig_10s_Song_02"] = "e063d38d-2e45-4b94-b63a-f541b6a7525b",
                        ["DecadePointOfInterestSongConfig_10s_Song_03"] = "91f4b593-45ae-4ff0-bc55-dcca49e32759",
                        ["DecadePointOfInterestSongConfig_10s_Song_04"] = "985ac433-1559-4abb-a266-9c6747fe960d",
                        ["DecadePointOfInterestSongConfig_10s_Song_05"] = "c841b7d6-bf9c-4c32-bce9-83da29dd5bdd",
                        ["DecadePointOfInterestSongConfig_20s_Song_01"] = "8dce1fc3-88fe-4db6-acf1-5607651948b1",
                        ["DecadePointOfInterestSongConfig_20s_Song_02"] = "ddfc646d-f7fc-4c91-88e4-8081fb288fd8",
                        ["DecadePointOfInterestSongConfig_20s_Song_03"] = "b2e05f5d-dae7-4d50-a338-522eca00727a",
                        ["DecadePointOfInterestSongConfig_20s_Song_04"] = "cf98c933-b52d-4f13-9204-6ef0cbe6de99",
                        ["DecadePointOfInterestSongConfig_20s_Song_05"] = "59ccee34-cf57-4198-be20-02591a97a7db",
                        ["DecadePointOfInterestSongConfig_20s_Song_06"] = "1a047d80-2c59-4d55-81e0-af4419eefc5e",
                        ["DecadePointOfInterestSongConfig_20s_Song_07"] = "ed925861-d119-4d43-ac7f-370ebdc479d9",
                        ["DecadePointOfInterestSongConfig_70s_Song_01"] = "bac7c194-f30c-49a6-8808-2e7d53ed8858",
                        ["DecadePointOfInterestSongConfig_70s_Song_02"] = "8f6f6200-8dd4-419a-958c-cd4a79cae561",
                        ["DecadePointOfInterestSongConfig_70s_Song_03"] = "85d4f1b4-675b-4f1d-b9fa-e477a15324a1",
                        ["DecadePointOfInterestSongConfig_70s_Song_04"] = "eeab7be1-2461-43ea-bec4-f86a7594bc7a",
                        ["DecadePointOfInterestSongConfig_70s_Song_05"] = "fb6b2aeb-4abe-4541-ba8a-eb632a8e54b4",
                        ["DecadePointOfInterestSongConfig_80s_Song_01"] = "efd3ef7c-3c45-439e-9c62-e92e4c9c2ccc",
                        ["DecadePointOfInterestSongConfig_80s_Song_02"] = "99f1114c-288b-4c95-9f7f-6e404aec2b29",
                        ["DecadePointOfInterestSongConfig_80s_Song_03"] = "4c88334c-dc02-4ade-a759-762c82d4c17b",
                        ["DecadePointOfInterestSongConfig_80s_Song_04"] = "044274ea-40c3-4673-bd23-69d0e4ac8a67",
                        ["DecadePointOfInterestSongConfig_80s_Song_05"] = "e662da6f-87d3-4392-90c5-3795f738014e",
                        ["DecadePointOfInterestSongConfig_90s_Song_01"] = "c2973935-ac3b-41c3-b3be-104679704539",
                        ["DecadePointOfInterestSongConfig_90s_Song_02"] = "0a1585b5-313b-402e-be86-1154dbe8a2ea",
                        ["DecadePointOfInterestSongConfig_90s_Song_03"] = "61d2ca98-c0de-4332-a2ad-5f840f37a5cc",
                        ["DecadePointOfInterestSongConfig_90s_Song_04"] = "2a76cd65-867f-4c41-89e7-984c45fd3fb1",
                        ["DecadePointOfInterestSongConfig_90s_Song_05"] = "cbe05732-05a7-4c6b-9a56-ab3cd695d44f",
                        ["DecadePointOfInterestSongConfig_2057_Song_01"] = "490c78b8-7f79-45b0-917f-5cf9ac04264a",
                        ["DecadePointOfInterestSongConfig_00s_Song_ALT_01"] = "6794b3fa-6427-47b8-820b-6bfe00abec28",
                        ["DecadePointOfInterestSongConfig_10s_Song_ALT_01"] = "b2b54eff-fb5e-4f42-82c3-84f4a1444c23",
                        ["DecadePointOfInterestSongConfig_20s_Song_ALT_01"] = "dd37b056-e4d2-428e-8eac-9e81fdb8c264",
                        ["DecadePointOfInterestSongConfig_20s_Song_ALT_02"] = "69b9f18b-2dec-4bb5-a7d5-5424105ce31e",
                        ["DecadePointOfInterestSongConfig_90s_Song_ALT_01"] = "aedc4687-e3ab-43a1-9ce0-28eb56bb61a1",
                    },
                    ["DecadeModeQuizBlacklist_Sorting"] = Array.Empty<object>(),
                    ["DecadeModeQuizBlacklist_TextMcq"] = Array.Empty<object>()
                }
            },
            ["Matchmaking"] = new()
            {
                Fields = new()
                {
                    ["GameMode"] = "test_type_1",
                    ["SaltRevision"] = JdNextConstants.MatchmakingSaltRevisionId,
                    ["ConfigRevision"] = 2,
                    ["AlgorithmParameters"] = new Dictionary<string, object>()
                }
            },
            ["MCAVersion"] = new()
            {
                Fields = new()
                {
                    ["Latest"] = "1.1.0",
                    ["Minimal"] = "1.0.0",
                    ["IncompatibleOSVersions"] = Array.Empty<object>()
                }
            },
            ["Multiplayer"] = new(),
            ["Onboarding"] = new()
            {
                Fields = new()
                {
                    ["PreselectionMaps"] = new List<string>()
                }
            },
            ["recommendations_activitypage"] = new()
            {
                Fields = new()
                {
                    ["Algo"] = "ALS",
                    ["typeAlgo"] = "ALS",
                    ["numberCtr"] = 0,
                    ["numberTagBased"] = 5
                }
            },
            ["recommendations_nextsteps"] = new()
            {
                Fields = new()
                {
                    ["AlgoType"] = "rss"
                }
            },
            ["ServerInfo"] = new()
            {
                Fields = new()
                {
                    ["url"] = $"https://{urlSettings.Value.HostUrl}",
                    ["name"] = "EFDS PROD"
                }
            },
            ["ServerSSLInfo"] = new()
            {
                Fields = new()
                {
                    ["publicKeyPinning"] = new PublicKeyPinning()
                }
            },
            ["SongsLibrary"] = new()
            {
                Fields = new()
                {
                    ["Filters"] = new Filters(tagService)
                }
            },
            ["Tracking"] = new()
            {
                Fields = new()
                {
                    ["ShouldTrackDetailedDancemoves"] = false
                }
            },
            ["UbiConnect"] = new()
            {
                Fields = new()
                {
                    ["getVisualNotificationUrl"] = $"https://{urlSettings.Value.HostUrl}/v1/profiles/{{profileId}}/global/ubiconnect/visualnotifications/api/{{visualNotificationType}}/{{visualNotificationId}}?spaceId={{spaceId}}",
                    ["visualNotificationButton"] = new Dictionary<string, string>
                    {
                        { "PS5", "ps5_triangle" },
                        { "Stadia", "None" },
                        { "Switch", "None" },
                        { "Keyboard", "kb_shiftf2" },
                        { "Scarlett", "xbx_Y" }
                    }
                }
            }
        }
    };
}
