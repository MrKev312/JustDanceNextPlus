using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("/v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/parameters")]
public class Parameters(TagService tagService) : ControllerBase
{
	[HttpGet(Name = "GetParametersSpaces")]
	public IActionResult GetParametersSpaces()
	{
		return Ok(new ParameterResponse(tagService));
	}
}

public class ParameterResponse(TagService tagService)
{
	public ParameterList Parameters { get; set; } = new(tagService);
}

public class ParameterList(TagService tagService)
{
	[JsonPropertyName("us-GlobalSpaceConfig")]
	public Parameter UsGlobalSpaceConfig { get; set; } = new()
	{
		Fields = new()
		{
			{"FriendsSpaceId", Guid.Parse("45d58365-547f-4b45-ab5b-53ed14cc79ed")},
			{"BlocklistSpaceId", Guid.Parse("45d58365-547f-4b45-ab5b-53ed14cc79ed")}
		}
	};

	[JsonPropertyName("us-sdkClientFeaturesSwitches")]
	public Parameter UsSdkClientFeaturesSwitches { get; set; } = new()
	{
		Fields = new()
		{
			{"ugc", true},
			{"news", true},
			{"tlog", true},
			{"event", true},
			{"mocks", false},
			{"party", true},
			{"stats", true},
			{"token", true},
			{"uplay", true},
			{"users", true},
			{"groups", true},
			{"trades", true},
			{"persona", true},
			{"rewards", true},
			{"calendar", true},
			{"profiles", true},
			{"blocklist", true},
			{"challenge", true},
			{"telemetry", true},
			{"voicechat", true},
			{"battlepass", true},
			{"httpClient", true},
			{"moderation", true},
			{"parameters", true},
			{"reputation", true},
			{"clubService", true},
			{"gamesPlayed", true},
			{"matchmaking", true},
			{"populations", true},
			{"localization", true},
			{"primaryStore", true},
			{"remoteGaming", true},
			{"richPresence", true},
			{"createSession", true},
			{"entitiesSpace", true},
			{"extendSession", true},
			{"friendsLookup", true},
			{"leaderboardMe", true},
			{"playerReports", true},
			{"friendsRequest", true},
			{"playerActivity", true},
			{"playerConsents", true},
			{"secondaryStore", true},
			{"ubisoftConnect", true},
			{"applicationUsed", true},
			{"clubApplication", true},
			{"entitiesProfile", true},
			{"recommendations", true},
			{"webSocketClient", true},
			{"clubDynamicPanel", true},
			{"notificationSend", true},
			{"playerPrivileges", true},
			{"usersLegalOptins", true},
			{"eventsDefinitions", true},
			{"groupsInvitations", true},
			{"leaderboardSpaces", true},
			{"playerPreferences", true},
			{"playerOnlineStatus", true},
			{"usersCreateAndLink", true},
			{"friendsUplayProfile", true},
			{"leaderboardProfiles", true},
			{"friendsSpaceSpecific", true},
			{"notificationSendBatch", true},
			{"notificationWebsocket", true},
			{"primaryStoreSendEvent", true},
			{"applicationInformation", true},
			{"notificationSendNoBroker", true},
			{"notificationDynamicUpdate", true},
			{"populationsAutomaticUpdate", true},
			{"primaryStoreAutomaticFetch", true},
			{"secondaryStoreTransactions", true},
			{"secondaryStoreInstantiation", true},
			{"secondaryStoreInventoryRules", true},
			{"mobileExtensionUsersManagement", true},
			{"notificationRequestConnections", true},
			{"mobileExtensionProfilesExternal", true},
			{"notificationRemoteLogReceivedData", false},
			{"primaryStoreCatalogGetGameMediaType", false},
			{"primarySecondaryStoreForceSyncAtLogin", true},
			{"firstPartyAccessToMultiplayerInformation", true},
			{"syncOnResumeToForegroundForNintendoSwitch", false},
			{"sanctions", true},
			{"usersPolicies", true},
			{"catalogDiscount", true},
			{"trackingSession ", false},
			{"playerCodeOfConduct", true}
			},
		RelatedPopulation = new()
		{
			Name = "Switch",
			Subject = "Platform"
		}
	};

	[JsonPropertyName("us-sdkClientMocks")]
	public Parameter? UsSdkClientMocks { get; set; }

	[JsonPropertyName("us-sdkClientNotificationsGame")]
	public Parameter UsSdkClientNotificationsGame { get; set; } = new()
	{
		Fields = new()
		{
			{"additionalSpaces", Array.Empty<object>()},
			{"JD_CUSTOM_BATCH_TEST", true},
			{"JD_CUSTOM_LEAVE_GROUP", true},
			{"JD_CUSTOM_ONLINE_TEST", true},
			{"JD_CUSTOM_GROUP_LEADER", true},
			{"JD_CUSTOM_GROUP_SERVER", true},
			{"JD_CUSTOM_KICKED_GROUP", true},
			{"JD_CUSTOM_PARTY_ACCESS", true},
			{"JD_CUSTOM_GROUP_METADATA", true},
			{"JD_CUSTOM_GROUP_DANCER_CARD", true},
			{"JD_CUSTOM_FRIEND_DANCER_CARD", true},
			{"JD_CUSTOM_NOSTIFICATION_TEST", true},
			{"JD_ONLINE_CUSTOM_NOTIFICATION", true},
			{"JD_CUSTOM_GROUP_MEMBER_CHANGED", true},
			{"UBICONNECT_VISUAL_NOTIFICATION", true},
			{"JD_CUSTOM_FIRSTPARTY_INVITE_SENT", true}
		}
	};

	[JsonPropertyName("us-sdkClientNotificationsInternal")]
	public Parameter UsSdkClientNotificationsInternal { get; set; } = new()
	{
		Fields = new()
		{
			{"BLOCKLIST_ADD", true},
			{"BLOCKLIST_ADDED", true},
			{"BLOCKLIST_REMOVE", true},
			{"BLOCKLIST_REMOVED", true},
			{"PARTY_MEMBER_ADDED", true},
			{"CLUB_BADGE_ACQUIRED", true},
			{"GROUPS_MEMBER_ADDED", true},
			{"PARTY_PARTY_CREATED", true},
			{"PARTY_PARTY_DELETED", true},
			{"PARTY_PARTY_UPDATED", true},
			{"FRIENDS_INFO_UPDATED", true},
			{"GROUPS_GROUP_DELETED", true},
			{"GROUPS_GROUP_UPDATED", true},
			{"PARTY_MEMBER_REMOVED", true},
			{"PARTY_MEMBER_UPDATED", true},
			{"SSI_INSTANCES_UPDATE", true},
			{"CLUB_ACTION_COMPLETED", true},
			{"CLUB_CHALLENGE_BANKED", true},
			{"CLUB_REWARD_PURCHASED", true},
			{"GROUPS_MEMBER_REMOVED", true},
			{"GROUPS_MEMBER_UPDATED", true},
			{"PARTY_INVITATION_SENT", true},
			{"PARTY_JOINREQUEST_SENT", true},
			{"BATTLEPASS_TIERS_BANKED", true},
			{"CLUB_CHALLENGE_COMPLETED", true},
			{"PARTY_INVITATION_EXPIRED", true},
			{"CHALLENGES_REWARDS_BANKED", true},
			{"PARTY_INVITATION_ACCEPTED", true},
			{"PARTY_INVITATION_DECLINED", true},
			{"PARTY_JOINREQUEST_EXPIRED", true},
			{"US_APP_PARAMS_FULL_UPDATE", true},
			{"GROUPS_GROUP_OWNER_UPDATED", true},
			{"PARTY_INVITATION_CANCELLED", true},
			{"PARTY_JOINREQUEST_ACCEPTED", true},
			{"PARTY_JOINREQUEST_DECLINED", true},
			{"PARTY_PARTY_EXPIRY_RENEWED", true},
			{"US_APP_PARAMS_GROUP_UPDATE", true},
			{"FRIENDS_RELATIONSHIP_UPDATE", true},
			{"PARTY_JOINREQUEST_CANCELLED", true},
			{"US_NOTIFICATION_MAINTENANCE", true},
			{"US_SPACE_PARAMS_FULL_UPDATE", true},
			{"CLUB_CHALLENGE_PARTICIPATION", true},
			{"FRIENDS_RELATIONSHIP_TRIGGER", true},
			{"US_SPACE_PARAMS_GROUP_UPDATE", true},
			{"PARTY_PARTY_LOCKSTATE_UPDATED", true},
			{"US_CONCURRENT_CONNECT_DETECTED", true},
			{"GROUPSINVITATIONS_GROUP_UPDATED", true},
			{"CHALLENGES_PROGRESSION_COMPLETED", true},
			{"CLUB_CHALLENGE_THRESHOLD_REACHED", true},
			{"US_CLIENT_SECONDARY_STORE_UPDATE", true},
			{"GROUPSINVITATIONS_INVITATION_SENT", true},
			{"PARTY_INVITATION_WORKFLOW_STARTED", true},
			{"GROUPSINVITATIONS_JOINREQUEST_SENT", true},
			{"GROUPSINVITATIONS_GROUP_LOCK_UPDATED", true},
			{"GROUPSINVITATIONS_INVITATION_EXPIRED", true},
			{"GROUPSINVITATIONS_INVITATION_ACCEPTED", true},
			{"GROUPSINVITATIONS_INVITATION_DECLINED", true},
			{"GROUPSINVITATIONS_JOINREQUEST_EXPIRED", true},
			{"GROUPSINVITATIONS_INVITATION_CANCELLED", true},
			{"GROUPSINVITATIONS_JOINREQUEST_ACCEPTED", true},
			{"GROUPSINVITATIONS_JOINREQUEST_DECLINED", true},
			{"GROUPS_POLICY_RETENTION_EXPIRY_RENEWED", true},
			{"PARTY_JOINREQUEST_PLAYER_PREAUTHORIZED", true},
			{"GROUPSINVITATIONS_JOINREQUEST_CANCELLED", true},
			{"TRADE_UPDATED", true},
			{"MM_ASK_GROUP_METADATA", true},
			{"FRIENDS_STATUS_CHANGED", true},
			{"PARTY_PARTY_MOVE_FAILED", true},
			{"PERSONA_PERSONA_CREATED", true},
			{"PERSONA_PERSONA_DELETED", true},
			{"PERSONA_PERSONA_UPDATED", true},
			{"BATTLEPASS_TIERS_REACHED", true},
			{"PARTY_PARTY_OWNER_UPDATED", true},
			{"UBICONNECT_REWARD_PURCHASED", false},
			{"PARTY_ACCESSREQUEST_ACCEPTED", true},
			{"PLAYERACTIVITY_ACTIVITIES_CLOSED", true},
			{"PLAYERACTIVITY_ACTIVITIES_CREATED", true},
			{"PLAYERACTIVITY_ACTIVITIES_UPDATED", true},
			{"PLAYERPRIVILEGES_PRIVILEGES_UPDATED", true},
			{"PARTY_LIMITS_MAXIMUM_MEMBERS_UPDATED", true},
			{"REPUTATION_PLAYERFACINGLEVEL_UPDATED", true},
			{"PARTY_PARTY_INVITATIONSCONFIG_UPDATED", true},
			{"PARTY_PARTY_JOINREQUESTSCONFIG_UPDATED", true},
			{"RICHPRESENCE_PLAYER_RICHPRESENCE_CHANGED", true}
		}
	};

	[JsonPropertyName("us-sdkClientNotificationsSpaceIds")]
	public Parameter UsSdkClientNotificationsSpaceIds { get; set; } = new()
	{
		Fields = new()
		{
			{"FriendsService", Guid.Parse("abb909cd-ebaa-4ba5-a4a5-afa75cae8195")},
			{"BlocklistService", Guid.Parse("45d58365-547f-4b45-ab5b-53ed14cc79ed")},
			{"PlayerPrivilegesService", Guid.Parse("2d2f687d-cf94-4757-9157-b8736284c438")}
		},
	};

	[JsonPropertyName("us-sdkClientRemoteLogsGame")]
	public Parameter UsSdkClientRemoteLogsGame { get; set; } = new()
	{
		Fields = new()
		{
			{"url", ""},
			{"maxTextLength", 32768},
			{"uncategorized", "Error"},
			{"ActivityPageService", "Error"}
		},
		RelatedPopulation = new()
		{
			Name = "Errors",
			Subject = "RemoteLogs"
		}
	};

	[JsonPropertyName("us-sdkClientRemoteLogsInternal")]
	public Parameter UsSdkClientRemoteLogsInternal { get; set; } = new()
	{
		Fields = new()
		{
			{"job", "Error"},
			{"ugc", "Error"},
			{"club", "Error"},
			{"core", "Error"},
			{"http", "Debug"},
			{"news", "Error"},
			{"tLog", "Error"},
			{"task", "Error"},
			{"test", "Error"},
			{"user", "Error"},
			{"async", "Error"},
			{"event", "Error"},
			{"mocks", "Error"},
			{"party", "Debug"},
			{"stats", "Error"},
			{"token", "Error"},
			{"uplay", "Error"},
			{"entity", "Error"},
			{"friend", "Error"},
			{"groups", "Debug"},
			{"trades", "Error"},
			{"overlay", "Error"},
			{"persona", "Error"},
			{"profile", "Error"},
			{"calendar", "Error"},
			{"blocklist", "Error"},
			{"challenge", "Error"},
			{"remoteLog", "Debug"},
			{"scheduler", "Error"},
			{"telemetry", "Error"},
			{"voicechat", "Error"},
			{"websocket", "Error"},
			{"battlepass", "Error"},
			{"connection", "Debug"},
			{"httpEngine", "Debug"},
			{"moderation", "Error"},
			{"parameters", "Error"},
			{"population", "Error"},
			{"reputation", "Error"},
			{"gamesPlayed", "Error"},
			{"leaderboard", "Error"},
			{"matchmaking", "Error"},
			{"userContent", "Error"},
			{"localization", "Error"},
			{"notification", "Error"},
			{"primaryStore", "Error"},
			{"remoteGaming", "Error"},
			{"richPresence", "Error"},
			{"configuration", "Error"},
			{"maxTextLength", 32768},
			{"playerReports", "Error"},
			{"authentication", "Error"},
			{"playerActivity", "Error"},
			{"playerConsents", "Error"},
			{"secondaryStore", "Error"},
			{"applicationUsed", "Error"},
			{"mobileExtension", "Error"},
			{"recommendations", "Error" },
			{"coreNotification", "Error"},
			{"playerPrivileges", "Error"},
			{"groupsInvitations", "Error"},
			{"playerPreferences", "Error"},
			{"playerOnlineStatus", "Error"},
			{"applicationInformation", "Error"},
			{"secondaryStoreInstantiation", "Error"},
			{"secondaryStoreInventoryRules", "Error"},
			{"sanctions", "None"},
			{"usersPolicies", "None"},
			{"catalogDiscount", "Warning"}
		},
		RelatedPopulation = new()
		{
			Name = "Errors",
			Subject = "RemoteLogs"
		}
	};

	[JsonPropertyName("us-sdkClientSettings")]
	public Parameter UsSdkClientSettings { get; set; } = new()
	{
		Fields = new()
		{
			{"popEventsTimeoutMsec", 5000},
			{"primaryStoreSyncDelayMsec", 250},
			{"createSessionRestPeriodMSec", 3000},
			{"createSessionRestRandomMSec", 5000},
			{"partyClientXblComplianceMethod", "mpsd"},
			{"secondaryStoreInventoryRulesMode", "Non-blocking"},
			{"xboxOneResumeFromSuspendedDelayMsec", 5000},
			{"enableCrossPlayPreferenceSyncForXbox", false},
			{"enablePartyClientFirstPartyCompliance", true},
			{"xboxOneCloseWebsocketOnSuspendingMode", "All"},
			{"notificationTypesUpdateRandomDelayMsec", 3000},
			{"waitRemoteLogCompletionOnDeleteSession", false},
			{"secondaryStoreInventoryRulesRetryDelayMsec", 5000},
			{"minBackgroundDurationForInventoryInvalidationMsec", 5000},
			{"createSessionRestPeriodAfterConcurrentConnectDetectedMSec", 60000}
		}
	};

	[JsonPropertyName("us-sdkClientSettingsCacheTTL")]
	public Parameter UsSdkClientSettingsCacheTtl { get; set; } = new()
	{
		Fields = new()
		{
			{"defaultSec", 120},
			{"spacesNewsSec", 900},
			{"friendsListSec", 1800},
			{"ssiRulesAllSec", 86400},
			{"profilesNewsSec", 900},
			{"profilesActionsSec", 120},
			{"profilesRewardsSec", 120},
			{"ssiAttributesAllSec", 86400},
			{"profilesChallengesSec", 120},
			{"ssiListsOfAttributesAllSec", 86400},
			{"challengesDefinitionSeasonSec", 180},
			{"battlepassSeasonProgressionSec", 60},
			{"profilesChallengesStatusSeasonSec", 60},
			{"friendsConfigSec", 1800},
			{"spacesParametersSec", 120},
			{"applicationsParametersSec", 120}
		}
	};

	[JsonPropertyName("us-sdkClientSettingsHttpGame")]
	public Parameter UsSdkClientSettingsHttpGame { get; set; } = new()
	{
		Fields = new()
		{
			{"maxCount", 3},
			{"retryMaxDelayMsec", 5000},
			{"retryRandomDelayMsec", 5000},
			{"retryInitialDelayMsec", 5000},
			{"timeoutInitialDelayMsec", 30000},
			{"retryIncrementFactorMsec", 5000},
			{"timeoutIncrementFactorMsec", 5000}
		}
	};

	[JsonPropertyName("us-sdkClientSettingsHttpInternal")]
	public Parameter UsSdkClientSettingsHttpInternal { get; set; } = new()
	{
		Fields = new()
		{
			{"maxCount", 3},
			{"retryMaxDelayMsec", 3600000},
			{"retryRandomDelay", 5000},
			{"retryInitialDelayMsec", 5000},
			{"timeoutInitialDelayMsec", 30000},
			{"retryIncrementFactorMsec", 5000},
			{"timeoutIncrementFactorMsec", 5000}
		}
	};

	[JsonPropertyName("us-sdkClientSettingsSecondaryStoreSync")]
	public Parameter UsSdkClientSettingsSecondaryStoreSync { get; set; } = new()
	{
		Fields = new()
		{
			{"maxCount", 10},
			{"restPeriodMsec", 30000},
			{"retryMaxDelayMsec", 3600000},
			{"retryRandomDelayMsec", 5000},
			{"retryInitialDelayMsec", 5000},
			{"retryIncrementFactorMsec", 5000},
			{"subscriptionAutomaticSyncPeriodInHoursGDK", 0}
		}
	};

	[JsonPropertyName("us-sdkClientSettingsWebSocketGame")]
	public Parameter UsSdkClientSettingsWebSocketGame { get; set; } = new()
	{
		Fields = new()
		{
			{"maxCount", 50},
			{"retryMaxDelayMsec", 5000},
			{"retryRandomDelayMsec", 5000},
			{"retryInitialDelayMsec", 5000},
			{"timeoutInitialDelayMsec", 30000},
			{"retryIncrementFactorMsec", 5000},
			{"connectionPingIntervalSec", 30},
			{"timeoutIncrementFactorMsec", 5000}
		}
	};

	[JsonPropertyName("us-sdkClientSettingsWebSocketInternal")]
	public Parameter UsSdkClientSettingsWebSocketInternal { get; set; } = new()
	{
		Fields = new()
		{
			{"maxCount", 50},
			{"retryMaxDelayMsec", 900000},
			{"retryRandomDelayMsec", 5000},
			{"retryInitialDelayMsec", 0},
			{"timeoutInitialDelayMsec", 30000},
			{"retryIncrementFactorMsec", 1500},
			{"connectionPingIntervalSec", 30},
			{"timeoutIncrementFactorMsec", 5000}
		}
	};

	[JsonPropertyName("us-sdkClientStorm")]
	public Parameter UsSdkClientStorm { get; set; } = new();

	[JsonPropertyName("us-sdkClientUrls")]
	public Parameter UsSdkClientUrls { get; set; } = new()
	{
		Fields = new()
		{
			{"news", "{baseurl_aws}/{version}/profiles/me/news"},
			{"tLog", "https://tglog.datamore.qq.com/{appId}/report/"},
			{"users", "{baseurl_aws}/{version}/users"},
			{"events", "{baseurl_aws}/{version}/profiles/{profileId}/events"},
			{"groups", "{baseurl_aws}/{version}/groups"},
			{"friends", "{baseurl_aws}/{version}/profiles/me/friends"},
			{"calendar", "{baseurl_aws}/{version}/spaces/{spaceId}/calendarentries"},
			{"policies", "{baseurl_aws}/{version}/policies"},
			{"profiles", "{baseurl_aws}/{version}/profiles"},
			{"sessions", "{baseurl_aws}/{version}/profiles/sessions"},
			{"blocklist", "{baseurl_aws}/{version}/profiles/{profileId}/blocks"},
			{"challenge", "{baseurl_aws}/{version}/spaces/{spaceId}/challenges"},
			{"groupType", "{baseurl_aws}/{version}/spaces/{spaceId}/grouptypes/{groupTypeId}"},
			{"sandboxes", "{baseurl_aws}/{version}/spaces/{spaceId}/sandboxes"},
			{"telemetry", "{baseurl_aws}/{version}/spaces/{spaceId}/global/tgdp/telemetry/gateway/data"},
			{"moderation", "{baseurl_aws}/{version}/spaces/{spaceId}/moderation/{text}"},
			{"remoteLogs", "{baseurl_aws}/{version}/profiles/me/remotelog"},
			{"spacesNews", "{baseurl_aws}/{version}/spaces/news"},
			{"tokenSpace", "{baseurl_aws}/{version}/spaces/{spaceId}/tokens"},
			{"connections", "{baseurl_aws}/{version}/profiles/{profileId}/connections"},
			{"gamesPlayed", "{baseurl_aws}/{version}/profiles/{profileId}/gamesplayed"},
			{"spacesItems", "{baseurl_aws}/{version}/spaces/{spaceId}/items"},
			{"spacesStats", "{baseurl_aws}/{version}/spaces/{spaceId}/communitystats"},
			{"applications", "{baseurl_aws}/{version}/applications/{applicationId}/configuration"},
			{"localization", "{baseurl_aws}/{version}/spaces/{spaceId}/localizations/strings"},
			{"personaSpace", "{baseurl_aws}/{version}/profiles/persona"},
			{"spacesOffers", "{baseurl_aws}/{version}/spaces/{spaceId}/offers"},
			{"tokenProfile", "{baseurl_aws}/{version}/profiles/{profileId}/tokens"},
			{"allgroupTypes", "{baseurl_aws}/{version}/spaces/{spaceId}/grouptypes"},
			{"calendarLists", "{baseurl_aws}/{version}/spaces/{spaceId}/calendarentrylists"},
			{"configsEvents", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/events"},
			{"groupsMembers", "{baseurl_aws}/{version}/groups/{groupId}/members"},
			{"partyXboxSync", "{baseurl_aws}/{version}/spaces/{spaceId}/parties/{partyId}/firstPartyParameters/xblMultiplayerSessionReferenceUri"},
			{"profilesStats", "{baseurl_aws}/{version}/profiles/{profileId}/stats"},
			{"profilesToken", "{baseurl_aws}/{version}/profiles/{profileId}/tokens/{token}"},
			{"spacesActions", "{baseurl_aws}/{version}/spaces/{spaceId}/actions"},
			{"spacesParties", "{baseurl_aws}/{version}/spaces/{spaceId}/parties"},
			{"spacesRewards", "{baseurl_aws}/{version}/spaces/{spaceId}/rewards"},
			{"allConnections", "{baseurl_aws}/{version}/profiles/connections"},
			{"allSpacesItems", "{baseurl_aws}/{version}/spaces/items"},
			{"moderationPOST", "{baseurl_aws}/{version}/spaces/{spaceId}/moderation"},
			{"personaProfile", "{baseurl_aws}/{version}/profiles/{profileId}/persona"},
			{"playerConsents", "{baseurl_aws}/{version}/profiles/{profileId}/consents/categories"},
			{"profilesGroups", "{baseurl_aws}/{version}/profiles/{profileId}/groups"},
			{"spacesEntities", "{baseurl_aws}/{version}/spaces/{spaceId}/entities"},
			{"allSpacesOffers", "{baseurl_aws}/{version}/spaces/offers"},
			{"localizationAll", "{baseurl_aws}/{version}/spaces/{spaceId}/localizations/strings/all"},
			{"profilesActions", "{baseurl_aws}/{version}/profiles/{profileId}/club/actions"},
			{"profilesFriends", "{baseurl_aws}/{version}/profiles/{profileId}/friends"},
			{"profilesParties", "{baseurl_aws}/{version}/profiles/{profileId}/parties"},
			{"profilesRewards", "{baseurl_aws}/{version}/profiles/{profileId}/club/rewards"},
			{"recommendations", "{baseurl_aws}/{version}/profiles/{profileId}/recommendations"},
			{"spacesStatsCard", "{baseurl_aws}/{version}/spaces/{spaceId}/communitystatscard"},
			{"websocketServer", "{baseurl_ws}"},
			{"allProfilesStats", "{baseurl_aws}/{version}/profiles/stats"},
			{"blocklistUnblock", "{baseurl_aws}/{version}/profiles/{profileId}/blocks/{blockedProfileId}"},
			{"profilesEntities", "{baseurl_aws}/{version}/profiles/{profileId}/entities"},
			{"profilesExternal", "{baseurl_aws}/{version}/profiles/external"},
			{"profilesMeEvents", "{baseurl_aws}/{version}/profiles/me/events"},
			{"profilesUgcViews", "{baseurl_aws}/{version}/profiles/ugc/{contentId}/views"},
			{"spacesChallenges", "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty"},
			{"spacesConfigsUgc", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/ugc"},
			{"spacesParameters", "{baseurl_aws}/{version}/spaces/{spaceId}/parameters"},
			{"tradesItemsGifts", "{baseurl_aws}/{version}/profiles/{profileId}/trades/itemsgifts"},
			{"tradesOfferGifts", "{baseurl_aws}/{version}/profiles/{profileId}/trades/offergifts"},
			{"allSpacesEntities", "{baseurl_aws}/{version}/spaces/entities"},
			{"eventsDefinitions", "{baseurl_aws}/{version}/spaces/{spaceId}/eventsDefinitions"},
			{"profilesInventory", "{baseurl_aws}/{version}/profiles/{profileId}/inventory"},
			{"profilesStatsCard", "{baseurl_aws}/{version}/profiles/statscard"},
			{"profilesUgcPhotos", "{baseurl_aws}/{version}/profiles/ugc/photos"},
			{"spacesLeaderboard", "{baseurl_aws}/{version}/spaces/{spaceId}/leaderboards"},
			{"blocklistBlockedBy", "{baseurl_aws}/{version}/profiles/{profileId}/blocks/blockedBy"},
			{"profilesChallenges", "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty"},
			{"profilesReputation", "{baseurl_aws}/{version}/profiles/{profileId}/reputation"},
			{"profilesUgcRatings", "{baseurl_aws}/{version}/profiles/{profileId}/ugc/ratings"},
			{"spacesBattlepasses", "{baseurl_aws}/{version}/spaces/{spaceId}/battlepasses"},
			{"allProfilesEntities", "{baseurl_aws}/{version}/profiles/entities"},
			{"gamesPlayedProfiles", "{baseurl_aws}/{version}/profiles/gamesplayed"},
			{"profilesLeaderboard", "{baseurl_aws}/{version}/profiles/ranks"},
			{"spacesRichPresences", "{baseurl_aws}/{version}/spaces/{spaceId}/richpresences"},
			{"usersOnlineStatuses", "{baseurl_aws}/{version}/users/onlineStatuses"},
			{"voicechatTokenVivox", "{baseurl_aws}/{version}/profiles/{profileId}/voicechattoken/vivox"},
			{"applicationsMetadata", "{baseurl_aws}/{version}/applications"},
			{"challengeProgression", "{baseurl_aws}/{version}/profiles/{profileId}/challenges/progressions"},
			{"playerReportsProfile", "{baseurl_aws}/{version}/profiles/{profileId}/reports"},
			{"profilesApplications", "{baseurl_aws}/{version}/profiles/me/applications"},
			{"profilesUgcFavorites", "{baseurl_aws}/{version}/profiles/{profileId}/ugc/favorites"},
			{"profilesUgcPhotosOwn", "{baseurl_aws}/{version}/profiles/{profileId}/ugc/photos"},
			{"spacesChallengepools", "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty"},
			{"voicechatConfigVivox", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/voicechat/vivox"},
			{"groupsRetentionExpiry", "{baseurl_aws}/{version}/groups/{groupId}/policies/retention/expiry"},
			{"profilesMeLeaderboard", "{baseurl_aws}/{version}/profiles/me/ranks"},
			{"profilesNotifications", "{baseurl_aws}/{version}/profiles/{profileId}/notifications"},
			{"spacesConfigsSsiRules", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/secondarystore/instances/rules"},
			{"usersMeOnlineStatuses", "{baseurl_aws}/{version}/users/me/onlineStatuses"},
			{"applicationsParameters", "{baseurl_aws}/{version}/applications/{applicationId}/parameters"},
			{"playerConsentsCategory", "{baseurl_aws}/{version}/profiles/{profileId}/consents/categories/{categoryName}"},
			{"spacesSeasonChallenges", "{baseurl_aws}/{version}/spaces/{spaceId}/club/seasonchallenges"},
			{"tradesItemsGiftsConfig", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/trades/itemsgifts"},
			{"tradesOfferGiftsConfig", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/trades/offergifts"},
			{"websocketNotifications", "{baseurl_ws}/{version}/websocket"},
			{"allProfilesApplications", "{baseurl_aws}/{version}/profiles/applications"},
			{"groupsInvitationsConfig", "{baseurl_aws}/{version}/groups/{groupId}/configs/groupsinvitations"},
			{"playerPrivilegesProfile", "{baseurl_aws}/{version}/profiles/{profileId}/playerprivileges"},
			{"profilesUgcUpdateRating", "{baseurl_aws}/{version}/profiles/{profileId}/ugc/{contentId}/ratings"},
			{"groupsInvitationsInvites", "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/invitations"},
			{"profilesSeasonChallenges", "{baseurl_aws}/{version}/profiles/{profileId}/club/seasonchallenges?spaceId={spaceId}"},
			{"profilesUgcReportContent", "{baseurl_aws}/{version}/profiles/{profileId}/ugc/{contentId}/reports"},
			{"groupsInvitationsProfiles", "{baseurl_aws}/{version}/profiles/{profileId}/groupsInvitations"},
			{"profilesMeRoamingProfiles", "{baseurl_aws}/{version}/users/me/roamingProfiles"},
			{"profilesProfileChallenges", "{baseurl_aws}/v2/spaces/global/ubiconnect/challenges/api/legacy/empty"},
			{"profilesUgcExternalVideos", "{baseurl_aws}/{version}/profiles/ugc/externalvideos"},
			{"profilesUgcUpdateFavorite", "{baseurl_aws}/{version}/profiles/{profileId}/ugc/{contentId}/favorites"},
			{"spacesBattlepassesSeasons", "{baseurl_aws}/{version}/spaces/{spaceId}/battlepasses/seasons"},
			{"spacesCommunityChallenges", "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty"},
			{"spacesConfigsPrimarystore", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/primarystore"},
			{"groupsInvitationsLockState", "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/lockstate"},
			{"profilesInventoryInstances", "{baseurl_aws}/{version}/profiles/{profileId}/inventory/instances"},
			{"profilesNotificationsBatch", "{baseurl_aws}/{version}/profiles/notifications"},
			{"spacesConfigsSsiAttributes", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/secondarystore/instances/attributes"},
			{"profilesBattlepassesSeasons", "{baseurl_aws}/{version}/profiles/{profileId}/battlepasses/seasons"},
			{"playerReportsSpaceCategories", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/reports/categories"},
			{"groupsInvitationsJoinRequests", "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/joinrequests"},
			{"groupsInvitationsUpdateInvite", "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/invitations/{inviteeProfileId}/state"},
			{"profilesInventoryPrimarystore", "{baseurl_aws}/{version}/profiles/{profileId}/inventory/primarystore"},
			{"profilesInventoryTransactions", "{baseurl_aws}/{version}/profiles/{profileId}/inventory/transactions"},
			{"profilesMeBattlepassesSeasons", "{baseurl_aws}/{version}/profiles/me/battlepasses/seasons"},
			{"profilesMeCommunityChallenges", "{baseurl_aws}/{version}/spaces/global/ubiconnect/challenges/api/legacy/empty"},
			{"matchmakingGroupsMatchesPrecise", "{baseurl_aws}/{version}/groups/{groupId}/matches/precise"},
			{"profilesInventoryExpiredDetails", "{baseurl_aws}/{version}/profiles/{profileId}/inventory/expiredDetails"},
			{"profilesMeInventoryPrimarystore", "{baseurl_aws}/{version}/profiles/me/inventory/primarystore"},
			{"profilesPreciseMatchmakingMatch", "{baseurl_aws}/{version}/profiles/{profileId}/matches/precise/matchstate"},
			{"spacesPlayerPreferencesStandard", "{baseurl_aws}/{version}/spaces/{spaceId}/standardpreferences"},
			{"profilesBattlepassesSeasonsTiers", "{baseurl_aws}/{version}/profiles/{profileId}/battlepasses/seasons/{seasonId}/tiers"},
			{"profilesPreciseMatchmakingClient", "{baseurl_aws}/{version}/profiles/{profileId}/matches/precise/clientstate"},
			{"profilesPlayerPreferencesStandard", "{baseurl_aws}/{version}/profiles/{profileId}/standardpreferences"},
			{"profilesUgcRequestReportedContent", "{baseurl_aws}/{version}/profiles/{profileId}/ugc/reports"},
			{"spacesBattlepassesSeasonsSeasonId", "{baseurl_aws}/{version}/spaces/{spaceId}/battlepasses/seasons/{seasonId}"},
			{"spacesConfigsSsiListsOfAttributes", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/secondarystore/instances/listsOfAttributes"},
			{"usersMeOnlineStatusesManualStatus", "{baseurl_aws}/{version}/users/me/onlineStatuses/manualStatus"},
			{"groupsInvitationsUpdateJoinRequest", "{baseurl_aws}/{version}/groups/{groupId}/groupsinvitations/joinrequests/{requesterProfileId}/state"},
			{"matchmakingSpaceGlobalHarboursocial", "{baseurl_aws}/{version}/spaces/{spaceId}/global/harboursocial/matchmaking"},
			{"spacesPartiesPartyIdMembersProfileId", "{baseurl_aws}/{version}/spaces/{spaceId}/parties/{partyId}/members/{profileId}"},
			{"profilesMeBattlepassesSeasonsSeasonId", "{baseurl_aws}/{version}/profiles/me/battlepasses/seasons/{seasonId}"},
			{"secondaryStoreInventoryRulesExecution", "{baseurl_aws}/{version}/profiles/{profileId}/inventory/rulesexecution"},
			{"matchmakingProfilesGlobalHarboursocial", "{baseurl_aws}/{version}/profiles/{profileId}/global/harboursocial/matchmaking"},
			{"profilesInventoryInstancesTransactions", "{baseurl_aws}/{version}/profiles/{profileId}/inventory/instances/transactions"},
			{"usersPolicies", "{baseurl_aws}/{version}/users/{userId}/policies"},
			{"friendsConfigs", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/friends"},
			{"trackingSession", "{baseurl_aws}/{version}/profiles/{profileId}/trackingSessions"},
			{"groupsRichPresences", "{baseurl_aws}/{version}/groups/{groupId}/richpresences"},
			{"spacesPlayerActivity", "{baseurl_aws}/{version}/spaces/{spaceId}/playeractivities/activities"},
			{"profilesRichPresences", "{baseurl_aws}/{version}/profiles/{profileId}/richpresences"},
			{"challengeManualBanking", "{baseurl_aws}/{version}/profiles/{profileId}/challenges/{challengeId}"},
			{"playerActivityProfiles", "{baseurl_aws}/{version}/profiles/{profileId}/playeractivities/activities"},
			{"ubiConnectRewardsSpace", "{baseurl_aws}/{version}/spaces/{spaceId}/global/ubiconnect/rewards/api"},
			{"voicechatConfigPlayfab", "{baseurl_aws}/{version}/spaces/{spaceId}/configs/voicechat/playfab"},
			{"profilesOffersDiscounts", "{baseurl_aws}/{version}/profiles/{profileId}/offersdiscounts"},
			{"voicechatNetworkPlayfab", "{baseurl_aws}/{version}/spaces/{spaceId}/voicechatnetworks/playfab"},
			{"playerConsentsNextConfig", "{baseurl_aws}/{version}/spaces/configs/consents"},
			{"ubiConnectRewardsProfile", "{baseurl_aws}/{version}/profiles/{profileId}/global/ubiconnect/rewards/api"},
			{"playerConsentsNextProfile", "{baseurl_aws}/{version}/profiles/{profileId}/consents"},
			{"profilesInventoryReserves", "{baseurl_aws}/{version}/profiles/{profileId}/items/reserves"},
			{"sanctionsAppliedSanctions", "{baseurl_aws}/{version}/profiles/{profileId}/sanctions"},
			{"playerConsentsNextAcceptances", "{baseurl_aws}/{version}/profiles/{profileId}/consents/acceptances"},
			{"profilesOffersDiscountsMatches", "{baseurl_aws}/{version}/profiles/{profileId}/offersdiscountsmatches"},
			{"profilesOffersDiscountsResolutions", "{baseurl_aws}/{version}/profiles/{profileId}/offersdiscountsresolutions"},
			{"ubiConnectCommunityChallengesSpace", "{baseurl_aws}/{version}/spaces/{spaceId}/global/ubiconnect/challenges/api/community"},
			{"ubiConnectCommunityChallengesProfile", "{baseurl_aws}/{version}/profiles/{profileId}/global/ubiconnect/challenges/api/community"},
			{"ubiConnectTimeLimitedChallengesSpace", "{baseurl_aws}/{version}/spaces/{spaceId}/global/ubiconnect/challenges/api/timelimited"},
			{"ubiConnectTimeLimitedChallengesProfile", "{baseurl_aws}/{version}/profiles/{profileId}/global/ubiconnect/challenges/api/timelimited"},
			{"ubiConnectApplicableTimeLimitedChallengesProfiles", "{baseurl_aws}/{version}/profiles/global/ubiconnect/challenges/api/timelimited/applicable"}
		}
	};

	[JsonPropertyName("us-simulation")]
	public Parameter UsSimulation { get; set; } = new()
	{
		Fields = new()
		{
			{"timeoffsetminutes", 0}
		}
	};

	[JsonPropertyName("ActivityPage")]
	public Parameter ActivityPage { get; set; } = new()
	{
		Fields = new()
		{
			{"Template", new Template()}
		},
		RelatedPopulation = new()
		{
			Name = "ActivityPage",
			Subject = "ActivityPage"
		}
	};

	public Parameter CustomClientFeaturesSwitches { get; set; } = new()
	{
		Fields = new()
		{
			{"CameraScoring", true},
			{"CameraScoringSurveyPopup", false}
		}
	};

	[JsonPropertyName("FirstPartyBundles")]
	public Parameter FirstPartyBundles { get; set; } = new();

	[JsonPropertyName("FirstPartySubscriptions")]
	public Parameter FirstPartySubscriptions { get; set; } = new();

	[JsonPropertyName("GameplaySettings")]
	public Parameter GameplaySettings { get; set; } = new()
	{
		Fields = new()
		{
			{"ClaimDisplayPriority", new List<string> { "songpack_year3", "songpack_year2", "songpack_year1", "jdplus" }},
			{"MCA_DeviceInclusionList", new Dictionary<string, object>()},
			{"CameraScoring_SurveyPopupSettings", new Dictionary<string, object>()},
			{"CameraScoring_MoveScoreBoostPercent", "11"},
			{"CameraScoring_MoveEvaluationTimeOffsetMs", "170"},
			{"CameraScoring_MoveScoreBoostPercent_SCA_iOS", "11"},
			{"CameraScoring_MoveEndToFeedbackDisplayDelayMs", "570"},
			{"CameraScoring_NoMoveMalusEnergyAmountThreshold", "0.25"},
			{"CameraScoring_CharityBonusEnergyFactorThreshold", "0.5"},
			{"CameraScoring_MoveScoreBoostPercent_SCA_Android", "12"},
			{"CameraScoring_MoveEvaluationTimeOffsetMs_SCA_iOS", "170"},
			{"CameraScoring_MoveEvaluationTimeOffsetMs_SCA_Android", "170"}
		},
		RelatedPopulation = new()
		{
			Name = "Switch",
			Subject = "Platform"
		}
	};

	[JsonPropertyName("Matchmaking")]
	public Parameter Matchmaking { get; set; } = new()
	{
		Fields = new()
		{
			{"GameMode", "test_type_1"},
			{"SaltRevision", Guid.Parse("884c590a-a36e-42c1-90c8-e24321921389")},
			{"ConfigRevision", 2},
			{"AlgorithmParameters", new Dictionary<string, object>()}
		}
	};

	[JsonPropertyName("MCAVersion")]
	public Parameter Mcaversion { get; set; } = new()
	{
		Fields = new()
		{
			{"Latest", "1.1.0"},
			{"Minimal", "1.0.0"}
		},
		RelatedPopulation = new()
		{
			Name = "Switch",
			Subject = "Platform"
		}
	};

	[JsonPropertyName("Multiplayer")]
	public Parameter Multiplayer { get; set; } = new();

	[JsonPropertyName("Onboarding")]
	public Parameter Onboarding { get; set; } = new()
	{
		Fields = new()
		{
			{"PreselectionMaps", new List<string>()}
		}
	};

	[JsonPropertyName("recommendations_activitypage")]
	public Parameter RecommendationsActivitypage { get; set; } = new()
	{
		Fields = new()
		{
			{"Algo", "ALSComplexTuned"},
			{"typeAlgo", "ALS"},
			{"numberCtr", 0},
			{"numberTagBased", 5}
		},
		RelatedPopulation = new()
		{
			Name = "ALSComplexTuned",
			Subject = "ABTestCategoriesALS"
		}
	};

	[JsonPropertyName("recommendations_nextsteps")]
	public Parameter RecommendationsNextsteps { get; set; } = new()
	{
		Fields = new()
		{
			{"AlgoType", "rss"}
		}
	};


	[JsonPropertyName("ServerInfo")]
	public Parameter ServerInfo { get; set; } = new()
	{
		Fields = new()
		{
			{"url", "https://prod-next.just-dance.com"},
			{"name", "EFDS PROD"}
		},
		RelatedPopulation = new()
		{
			Name = "Public",
			Subject = "Endpoint"
		}
	};

	[JsonPropertyName("ServerSSLInfo")]
	public Parameter ServerSslInfo { get; set; } = new()
	{
		Fields = new()
		{
			{"PublicKeyPinning", new PublicKeyPinning()}
		}
	};

	[JsonPropertyName("ShopV1DisplayConfig")]
	public Parameter ShopV1DisplayConfig { get; set; } = new();

	[JsonPropertyName("SongsLibrary")]
	public Parameter SongsLibrary { get; set; } = new()
	{
		Fields = new()
		{
			{"Filters", new Filters(tagService)}
		}
	};

	[JsonPropertyName("Tracking")]
	public Parameter Tracking { get; set; } = new()
	{
		Fields = new()
		{
			{"ShouldTrackDetailedDancemoves", false}
		},
		RelatedPopulation = new()
		{
			Name = "DetailedDancemoveTrackingDisabled",
			Subject = "DetailedDancemoveTracking"
		}
	};

	[JsonPropertyName("UbiConnect")]
	public Parameter UbiConnect { get; set; } = new()
	{
		Fields = new()
		{
			{ "getVisualNotificationUrl", "https://public-ubiservices.ubi.com/v1/profiles/{profileId}/global/ubiconnect/visualnotifications/api/{visualNotificationType}/{visualNotificationId}?spaceId={spaceId}" },
			{ "visualNotificationButton", new Dictionary<string, string>
			{
				{ "PS5", "ps5_triangle" },
				{ "Stadia", "None" },
				{ "Switch", "None" },
				{ "Keyboard", "kb_shiftf2" },
				{ "Scarlett", "xbx_Y" }
			} }
		}
	};
}

public class Parameter
{
	public Dictionary<string, object> Fields { get; set; } = [];
	public RelatedPopulation? RelatedPopulation { get; set; }
}

public class Template
{
	[JsonPropertyName("template")]
	public List<TemplateItem> TemplateItems { get; set; } =
	[
		new("song", "carousel"),
		new("playlist", "carousel"),
		new("banner"),
		new("song", "carousel"),
		new("playlist", "carousel"),
		new("song", "carousel"),
		new("banner"),
		new("song", "carousel"),
		new("song", "carousel"),
		new("song", "carousel"),
		new("playlist", "carousel"),
		new("banner"),
		new("song", "carousel"),
		new("song", "carousel"),
		new("playlist", "carousel"),
		new("song", "carousel"),
		new("banner"),
		new("song", "carousel"),
		new("song", "carousel"),
		new("song", "carousel")
	];
}

public class TemplateItem
{
	public TemplateItem(string categorytype)
	{
		Categorytype = categorytype;
	}

	public TemplateItem(string itemtype, string categorytype)
	{
		Itemtype = itemtype;
		Categorytype = categorytype;
	}

	public string Itemtype { get; set; } = "";
	public string Bannertype { get; set; } = "";
	public string Categorytype { get; set; } = "";
}

public class PublicKeyPinning
{
	public List<PinningKey> PinningKeys { get; set; } = [];
}

public class PinningKey
{
	public string Endpoint { get; set; } = "";
	public string Description { get; set; } = "";
	public List<string> PublicKeyDigests { get; set; } = [];
}

public class Filters(TagService tagService)
{

	[JsonPropertyName("filters")]
	public List<Filter> FilterList { get; set; } = tagService.GetFilters();
}

public class RelatedPopulation
{
	public string? Name { get; set; }
	public string? Subject { get; set; }
}
