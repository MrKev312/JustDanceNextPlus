using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("v1/spaces/{spaceId:jdnext}/items")]
public class Items(ILockerItemsService lockerItemsService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetItems([FromRoute] Guid spaceId, [FromQuery] string type, [FromQuery] int limit, [FromQuery] int offset)
	{
		if (string.IsNullOrEmpty(type))
			return BadRequest("Type is required");

		if (!lockerItemsService.LockerItems.TryGetValue(type, out IReadOnlyList<LockerItem>? items))
			return NotFound($"No items found for type {type}");

		return Ok(new
		{
			items = items.Skip(offset).Take(limit).ToList()
		});
	}
}
