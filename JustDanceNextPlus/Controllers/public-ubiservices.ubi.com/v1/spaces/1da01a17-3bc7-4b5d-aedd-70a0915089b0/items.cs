using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

using System.Text.Json.Nodes;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/items")]
public class Items(LockerItemsService lockerItemsService) : ControllerBase
{
	[HttpGet]
	public IActionResult GetItems([FromQuery] string type, [FromQuery] int limit, [FromQuery] int offset)
	{
		if (string.IsNullOrEmpty(type))
			return BadRequest("Type is required");

		if (!lockerItemsService.LockerItems.TryGetValue(type, out List<LockerItem>? items))
			return NotFound($"No items found for type {type}");

		return Ok(new
		{
			items = items.Skip(offset).Take(limit).ToList()
		});
	}
}
