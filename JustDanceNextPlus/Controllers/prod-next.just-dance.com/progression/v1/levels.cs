using JustDanceNextPlus.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace JustDanceNextPlus.Controllers.prod_next.just_dance.com.progression.v1;

[ApiController]
[Route("progression/v1/levels")]
public class Levels(IOptions<PathSettings> pathSettings) : ControllerBase
{
	[HttpGet(Name = "GetLevels")]
	public IActionResult GetLevels()
	{
		string levelsJson = Path.Combine(pathSettings.Value.JsonsPath, "levels.json");
		if (System.IO.File.Exists(levelsJson))
		{
			FileStream stream = System.IO.File.OpenRead(levelsJson);
			return File(stream, "application/json");
		}

		LevelsConfig levelsConfig = new();

		for (int i = 0; i < 150; i++)
		{
			int levelIndex = (i % levelsConfig.LevelsPerPrestige) + 1;

			PerLevelConfig perLevelConfig = new()
			{
				Xp = 100 * (levelIndex - 1) * (levelIndex + 10)
			};

			levelsConfig.PerLevelConfig.Add(i + 1, perLevelConfig);
		}

		return Ok(levelsConfig);
	}
}

public class LevelsConfig
{
	public SortedDictionary<int, PerLevelConfig> PerLevelConfig { get; set; } = [];
	public int LevelsPerPrestige { get; set; } = 50;
	public int HiddenLevelXP { get; set; } = 305000;
}

public class PerLevelConfig
{
	public int Xp { get; set; } = 0;
	public List<Guid> Reward { get; set; } = [];
}