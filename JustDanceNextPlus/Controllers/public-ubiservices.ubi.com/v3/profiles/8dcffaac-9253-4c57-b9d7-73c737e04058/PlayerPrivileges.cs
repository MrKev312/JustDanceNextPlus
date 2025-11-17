using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v2.profiles._afcb4bd7_eae8_40fe_bb4a_8a03c16ad95f;

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
        		"30101a43-422c-4c6b-ab19-e6ea319a5dfa": {
        			"displayValues": {
        				"name": "AccessGame",
        				"description": "AccessGame"
        			},
        			"computedValue": {
        				"reasonId": "f6cc52d5-8250-483f-8975-75b30dd65157",
        				"isGranted": true
        			},
        			"reasons": [{
        				"displayValues": {
        					"name": "globalDefaultReason",
        					"description": "Global default reason description"
        				},
        				"reasonId": "f6cc52d5-8250-483f-8975-75b30dd65157",
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
