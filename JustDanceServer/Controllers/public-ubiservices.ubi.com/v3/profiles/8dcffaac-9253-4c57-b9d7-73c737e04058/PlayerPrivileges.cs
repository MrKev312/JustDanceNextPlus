using JustDanceServer.Services;

using JustDanceServer.Configuration;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v2.profiles._afcb4bd7_eae8_40fe_bb4a_8a03c16ad95f;

[ApiController]
[Route("v2/profiles/{guid:guid}/playerprivileges")]
public class PlayerPrivileges(ITimingService timingService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetPlayerPrivileges([FromRoute] Guid guid)
    {
        string response = $$"""
        {
        	"privileges": {
                "{{JdNextConstants.AccessGamePrivilegeId}}": {
        			"displayValues": {
        				"name": "AccessGame",
        				"description": "AccessGame"
        			},
        			"computedValue": {
                        "reasonId": "{{JdNextConstants.GlobalDefaultPrivilegeReasonId}}",
        				"isGranted": true
        			},
        			"reasons": [{
        				"displayValues": {
        					"name": "globalDefaultReason",
        					"description": "Global default reason description"
        				},
                        "reasonId": "{{JdNextConstants.GlobalDefaultPrivilegeReasonId}}",
        				"isGranted": true,
        				"isDefault": true,
        				"lastModifiedAt": "{{timingService.ServerTime()}}"
        			}],
        			"lastModifiedAt": "{{timingService.ServerTime()}}"
        		}
        	}
        }
        """;

        return new ContentResult
        {
            Content = response,
            ContentType = "application/json"
        };
    }
}
