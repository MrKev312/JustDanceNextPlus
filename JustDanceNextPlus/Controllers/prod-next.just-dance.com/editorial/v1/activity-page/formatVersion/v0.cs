using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.editorial.v1.activity_page.formatVersion;

[ApiController]
[Route("editorial/v1/activity-page/formatVersion/v0")]
public class V0(IActivityPageService activityPageService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetActivityPage()
	{
		ActivityPageResponse? activityPage = activityPageService.ActivityPage;
		return activityPage == null 
			? NotFound() 
			: Ok(activityPage);
	}
}