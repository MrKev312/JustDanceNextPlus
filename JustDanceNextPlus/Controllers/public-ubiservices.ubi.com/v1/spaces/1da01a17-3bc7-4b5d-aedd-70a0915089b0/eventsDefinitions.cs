using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("v1/spaces/1da01a17-3bc7-4b5d-aedd-70a0915089b0/eventsDefinitions")]
public class EventsDefinitions(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet]
	public IActionResult GetEventsDefinitions()
	{
		string eventsDefinitionsJson = Path.Combine(pathSettings.Value.JsonsPath, "eventsDefinitions.json");

		if (System.IO.File.Exists(eventsDefinitionsJson))
		{
			FileStream stream = System.IO.File.OpenRead(eventsDefinitionsJson);
			return File(stream, "application/json");
		}

		return Ok(new EventsDefinitionsResponse());
	}
}

public class EventsDefinitionsResponse
{
	public List<AttributeDefinition> Attributes { get; set; } = [];
	public List<CompositionDefinition> Compositions { get; set; } = [];
	public List<SignalDefinition> Signals { get; set; } = [];
}

public class AttributeDefinition
{
	public string Name { get; set; } = "Placeholder";
	public string Type { get; set; } = "Placeholder";
	public bool IsOptional { get; set; }
}

public class CompositionDefinition
{
	public string Name { get; set; } = "Placeholder";
	public string Type { get; set; } = "Placeholder";
	public List<Attribute> TypeAttributes { get; set; } = [];
	public List<Attribute> CustomAttributes { get; set; } = [];
}

public class Attribute
{
	public string Name { get; set; } = "Placeholder";
}

public class SignalDefinition
{
	public string Name { get; set; } = "Placeholder";
	public List<Attribute> Compositions { get; set; } = [];
}
