using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.profiles.other;

[ApiController]
[Route("v1/profiles/{guid:guid}/inventory")]
public class Inventory(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet(Name = "GetInventory")]
	public IActionResult GetInventory()
	{
		string inventoryJson = Path.Combine(pathSettings.Value.JsonsPath, "inventory.json");
		return System.IO.File.Exists(inventoryJson)
			? Ok(System.IO.File.ReadAllText(inventoryJson))
			: Ok(new InventoryResponse());
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
