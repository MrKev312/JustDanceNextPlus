using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.editorial.v1;

[ApiController]
[Route("editorial/v1/tagdb")]
public class TagDB(ITagService tagService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetTagdb()
	{
		return Ok(tagService.TagDatabase);
	}
}
