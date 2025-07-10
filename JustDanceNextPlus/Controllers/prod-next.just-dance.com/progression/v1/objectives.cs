﻿using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1;

[ApiController]
[Route("progression/v1/objectives")]
public class Objectives(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet(Name = "GetObjectives")]
	public IActionResult GetObjectives()
	{
		string objectivesJson = Path.Combine(pathSettings.Value.JsonsPath, "objectives.json");

		if (System.IO.File.Exists(objectivesJson))
		{
			FileStream stream = System.IO.File.OpenRead(objectivesJson);
			return File(stream, "application/json");
		}

		return Ok(new Tasks());
	}
}

public class Tasks
{
	public SortedDictionary<Guid, Task> TaskList { get; set; } = [];
}

public class Task
{
	public Level[] Levels { get; set; } = [];
	public string Name { get; set; } = "Placeholder";
	public int NameLocId { get; set; }
	public int Type { get; set; }
}

public class Level
{
	public ConfigVariables ConfigVariables { get; set; } = new();
	public string Description { get; set; } = "Placeholder";
	public int DescriptionLocId { get; set; }
	public Guid Id { get; set; }
	public LocVariableIds LocVariableIds { get; set; } = new();
	public LocVariableTagIds LocVariableTagIds { get; set; } = new();
	public LocVariables LocVariables { get; set; } = new();
	public string Name { get; set; } = "Placeholder";
	public int NameLocId { get; set; }
	public Reward Reward { get; set; } = new();
	public int TotalSteps { get; set; }
	public int Type { get; set; }
}

public class ConfigVariables
{
	public Guid MapTagToMatch { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? StarsToReach { get; set; } = null;
}

public class LocVariableIds
{
	public int SCORETOREACHLOCID { get; set; }
}

public class LocVariableTagIds
{
	public Guid TAGSONGFILTER { get; set; }
}

public class LocVariables
{
	public int OBJTIMES { get; set; }
}

public class Reward
{
	public List<Guid> Items { get; set; } = [];
	public int Xp { get; set; }
}
