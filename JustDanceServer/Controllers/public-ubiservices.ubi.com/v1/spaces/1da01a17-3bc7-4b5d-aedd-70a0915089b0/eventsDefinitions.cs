using JustDanceServer.Configuration;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceServer.Controllers.public_ubiservices.ubi.com.v1.spaces._1da01a17_3bc7_4b5d_aedd_70a0915089b0;

[ApiController]
[Route("v1/spaces/{spaceId:jdnext}/eventsDefinitions")]
public class EventsDefinitions(IOptions<PathSettings> pathSettings,
    IFileSystem fileSystem) : ControllerBase
{
    [HttpGet]
    public IActionResult GetEventsDefinitions([FromRoute] Guid spaceId)
    {
        string eventsDefinitionsJson = Path.Combine(pathSettings.Value.JsonsPath, "eventsDefinitions.json");

        return fileSystem.FileExists(eventsDefinitionsJson)
            ? File(fileSystem.OpenRead(eventsDefinitionsJson), "application/json")
            : Ok(new EventsDefinitionsResponse());
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
