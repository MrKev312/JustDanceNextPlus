using JustDanceNextPlus.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.profiles.other;

[ApiController]
[Route("v1/profiles/{guid:guid}/inventory")]
public class Inventory(LockerItemsService lockerItemsService) : ControllerBase
{
	[HttpGet(Name = "GetInventory")]
	public IActionResult GetInventory()
	{
		InventoryResponse inventoryResponse = new()
		{
			Items = [.. lockerItemsService.LockerItemIds.Select(itemId => new Item
			{
				ItemId = itemId,
				Quantity = 1,
				LastModified = DateTime.UtcNow - TimeSpan.FromDays(1),
			})]
		};

		return Ok(inventoryResponse);
	}
}

public class InventoryResponse
{
	public List<Item> Items { get; set; } = [];
}

public class Item
{
	public Guid ItemId { get; set; }
	public int Quantity { get; set; }
	public DateTime? ExpirationDate { get; set; } = null;
	public DateTime LastModified { get; set; }
	public string? ExpirationDetails { get; set; } = null;
}
