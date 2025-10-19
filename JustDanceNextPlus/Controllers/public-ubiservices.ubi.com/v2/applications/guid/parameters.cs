using JustDanceNextPlus.Configuration;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v2.applications.guid;

[ApiController]
[Route("/v2/applications/{Guid:guid}/parameters")]
public class Parameters(IOptions<UrlSettings> urlSettings) : ControllerBase
{
	[HttpGet(Name = "GetParameters")]
	public IActionResult GetParameters()
	{
		return Ok(parameters);
	}

	readonly ParameterList parameters = new()
	{
		Parameters = new()
		{
			["us-staging"] = new()
			{
				Fields = new()
				{
					["spaceId"] = Guid.Parse("1da01a17-3bc7-4b5d-aedd-70a0915089b0")
				},
				RelatedPopulation = new()
				{
					Name = "prod",
					Subject = "us_staging"
				}
			},
			["us-sdkClientClub"] = new()
			{
				Fields = new()
				{
					["dynamicPanelUrl"] = $$"""https://{env}{{urlSettings.Value.HostUrl}}/v1/profiles/{profileId}/club/dynamicPanel/MyProfile.png?spaceId={spaceId}&noRedirect=true""",
					["switchClubWebsiteUrl"] = "https://static8.cdn.ubi.com/u/sites/UbisoftClub/NintendoSwitch/index.html?url=/games/{deepLink}&ussdkversion={usSdkVersionName}&env={clubEnvName}&actioncompleted={actionCompletedList}&forceLang={forceLang}&profileId={profileId}&ticket={ticket}&context={context}&debug={debug}&spaceId={spaceId}&applicationId={applicationId}"
				}
			},
			["us-sdkClientRemoteGaming"] = new()
			{
				Fields = new()
				{
					["remoteGamingNetworkDelayTTLSec"] = 1
				}
			},
			["us-sdkClientGlobalAppConfig"] = new()
			{
				Fields = new()
				{
					["mobileDeviceEmailsDaysTTL"] = 90
				}
			},
			["us-sdkClientHttpConfig"] = new()
			{
				Fields = new()
				{
					["maxConcurrentUsRequests"] = null
				}
			},
			["us-sdkClientTelemetry"] = new()
			{
				Fields = new()
				{
					["categories"] = new Dictionary<string, object>()
				}
			},
			["us-sdkClientUrlsPlaceholders"] = new()
			{
				Fields = new()
				{
					["baseurl_ws"] = new Dictionary<string, string>()
					{
						["Standard"] = $$"""wss://{env}{{urlSettings.Value.HostUrl}}"""
					},
					["baseurl_aws"] = new Dictionary<string, string>()
					{
						["Standard"] = $$"""https://{env}{{urlSettings.Value.HostUrl}}"""
					},
					["baseurl_msr"] = new Dictionary<string, string>()
					{
						["Standard"] = $$"""https://msr-{env}{{urlSettings.Value.HostUrl}}"""
					}
				}
			},
			["us-sdkClientLogin"] = new()
			{
				Fields = new()
				{
					["populationHttpRequestOptimizationEnabled"] = true,
					["populationHttpRequestOptimizationRetryCount"] = 0,
					["populationHttpRequestOptimizationRetryTimeoutIntervalMsec"] = 5000,
					["populationHttpRequestOptimizationRetryTimeoutIncrementMsec"] = 1000
				}
			},
			["us-sdkClientFeaturesSwitches"] = new()
			{
				Fields = new()
				{
					["populationsAutomaticUpdate"] = true,
					["forcePrimarySecondaryStoreSyncOnReturnFromBackground"] = true
				}
			},
			["us-sdkClientFleet"] = new()
			{
				Fields = new()
				{
					["title"] = "",
					["spaces"] = $$"""https://{env}{{urlSettings.Value.HostUrl}}/{version}/spaces/{spaceId}/title/{title}/""",
					["profiles"] = $$"""https://{env}{{urlSettings.Value.HostUrl}}/{version}/profiles/{profileId}/title/{title}/""",
					["spaces_all"] = $$"""https://{env}{{urlSettings.Value.HostUrl}}/{version}/spaces/title/{title}/""",
					["applications"] = $$"""https://{env}{{urlSettings.Value.HostUrl}}/{version}/applications/{applicationId}/title/{title}/""",
					["profiles_all"] = $$"""https://{env}{{urlSettings.Value.HostUrl}}/{version}/profiles/title/{title}/""",
					["applications_all"] = $$"""https://{env}{{urlSettings.Value.HostUrl}}/{version}/applications/title/{title}/"""
				}
			},
			["us-sdkClientUrls"] = new()
			{
				Fields = new()
				{
					["populations"] = "{baseurl_aws}/{version}/profiles/me/populations",
					["profilesToken"] = "{baseurl_aws}/{version}/profiles/{profileId}/tokens/{token}"
				}
			}
		}
	};
}
