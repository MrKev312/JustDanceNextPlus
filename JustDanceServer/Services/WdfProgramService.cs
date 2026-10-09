using JustDanceServer.Configuration;

using Microsoft.Extensions.Options;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceServer.Services;

[JsonConverter(typeof(JsonStringEnumConverter<WdfProgramKind>))]
public enum WdfProgramKind
{
	BossBattle,
	TeamBattle,
	Tournament
}

public sealed record WdfProgramDefinition
{
	public required string Id { get; init; }
	public required WdfProgramKind Kind { get; init; }
	public string[] Rooms { get; init; } = [];
	public string[][] MapSets { get; init; } = [];
	public int RoundDurationSeconds { get; init; } = 180;
	public WdfBossDefinition? Boss { get; init; }
	public WdfTeamDefinition[] Teams { get; init; } = [];
	public WdfTournamentDefinition? Tournament { get; init; }
}

public sealed record WdfBossDefinition
{
	public required string Name { get; init; }
	public long Health { get; init; } = 1_000_000;
	public string RewardKey { get; init; } = string.Empty;
}

public sealed record WdfTeamDefinition
{
	public required string Id { get; init; }
	public required string Name { get; init; }
	public string Color { get; init; } = "FFFFFFFF";
	public string AssetUrl { get; init; } = string.Empty;
}

public sealed record WdfTournamentDefinition
{
	public int MaximumParticipants { get; init; } = 16;
	public int PlayersPerMatch { get; init; } = 2;
}

public interface IWdfProgramService : ILoadService
{
	IReadOnlyCollection<WdfProgramDefinition> Definitions { get; }
	WdfProgramRuntime CreateRuntime(string programId, WdfRoomId room);
}

public sealed class WdfProgramService(
	IOptions<PathSettings> pathSettings,
	JsonSettingsService jsonSettings,
	IMapService mapService,
	IFileSystem fileSystem,
	TimeProvider timeProvider,
	ILogger<WdfProgramService> logger) : IWdfProgramService
{
	private IReadOnlyDictionary<string, WdfProgramDefinition> definitions =
		new Dictionary<string, WdfProgramDefinition>(StringComparer.OrdinalIgnoreCase);

	public IReadOnlyCollection<WdfProgramDefinition> Definitions => [.. definitions.Values];

	public async Task LoadData()
	{
		string path = pathSettings.Value.WdfProgramsPath;
		if (string.IsNullOrWhiteSpace(path) || !fileSystem.DirectoryExists(path))
		{
			definitions = new Dictionary<string, WdfProgramDefinition>(StringComparer.OrdinalIgnoreCase);
			return;
		}

		List<WdfProgramDefinition> loaded = [];
		HashSet<string> loadedIds = [with(StringComparer.OrdinalIgnoreCase)];
		foreach (string file in fileSystem.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly)
			.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
		{
			await using Stream stream = fileSystem.OpenRead(file);
			WdfProgramDefinition[] values = await JsonSerializer.DeserializeAsync<WdfProgramDefinition[]>(
				stream,
				jsonSettings.PrettyPascalFormat) ?? [];
			foreach (WdfProgramDefinition value in values)
			{
				Validate(value, file);
				if (!loadedIds.Add(value.Id))
					throw new InvalidOperationException($"Duplicate WDF program ID '{value.Id}'.");
			}

			loaded.AddRange(values);
		}

		definitions = loaded.ToDictionary(value => value.Id, StringComparer.OrdinalIgnoreCase);
		logger.LogInformation("Loaded {Count} dormant WDF program definitions.", definitions.Count);
	}

	public WdfProgramRuntime CreateRuntime(string programId, WdfRoomId room)
	{
		if (!definitions.TryGetValue(programId, out WdfProgramDefinition? definition))
			throw new KeyNotFoundException($"Unknown WDF program '{programId}'.");
		if (definition.Rooms.Length > 0
			&& !definition.Rooms.Contains(room.Name, StringComparer.OrdinalIgnoreCase))
			throw new InvalidOperationException($"Program '{programId}' does not support room '{room.Name}'.");

		return new WdfProgramRuntime(definition, room, timeProvider.GetUtcNow());
	}

	private void Validate(WdfProgramDefinition definition, string file)
	{
		if (string.IsNullOrWhiteSpace(definition.Id))
			throw new InvalidOperationException($"A WDF program in '{file}' has no ID.");
		if (definition.RoundDurationSeconds <= 0)
			throw new InvalidOperationException($"WDF program '{definition.Id}' has an invalid duration.");
		foreach (string room in definition.Rooms)
		{
			if (!WdfRoomId.TryParse(room, out _))
				throw new InvalidOperationException($"WDF program '{definition.Id}' has invalid room '{room}'.");
		}

		foreach (string mapName in definition.MapSets.SelectMany(value => value))
		{
			if (mapService.GetMap(mapName) is null)
				throw new InvalidOperationException($"WDF program '{definition.Id}' references unknown map '{mapName}'.");
		}

		switch (definition.Kind)
		{
			case WdfProgramKind.BossBattle when definition.Boss is null || definition.Boss.Health <= 0:
				throw new InvalidOperationException($"Boss program '{definition.Id}' needs positive-health boss data.");
			case WdfProgramKind.TeamBattle when definition.Teams.Length < 2:
				throw new InvalidOperationException($"Team program '{definition.Id}' needs at least two teams.");
			case WdfProgramKind.Tournament when definition.Tournament is null
				|| definition.Tournament.PlayersPerMatch < 2
				|| definition.Tournament.MaximumParticipants < definition.Tournament.PlayersPerMatch:
				throw new InvalidOperationException($"Tournament program '{definition.Id}' has invalid bracket data.");
		}
	}
}

public sealed class WdfProgramRuntime(
	WdfProgramDefinition definition,
	WdfRoomId room,
	DateTimeOffset startedAt)
{
	private readonly Lock stateLock = new();
	private readonly Dictionary<Guid, long> playerScores = [];

	public WdfProgramDefinition Definition { get; } = definition;
	public WdfRoomId Room { get; } = room;
	public DateTimeOffset StartedAt { get; } = startedAt;
	public WdfModeRuntime Mode { get; } = definition.Kind switch
	{
		WdfProgramKind.BossBattle => new WdfBossRuntime(definition.Boss!),
		WdfProgramKind.TeamBattle => new WdfTeamBattleRuntime(definition.Teams),
		WdfProgramKind.Tournament => new WdfTournamentRuntime(definition.Tournament!),
		_ => throw new ArgumentOutOfRangeException(nameof(definition))
	};

	public void SubmitScore(Guid profileId, long score)
	{
		if (profileId == Guid.Empty || score < 0)
			throw new ArgumentOutOfRangeException(nameof(score));
		lock (stateLock)
		{
			long previous = playerScores.GetValueOrDefault(profileId);
			long delta = Math.Max(0, score - previous);
			playerScores[profileId] = Math.Max(previous, score);
			Mode.ApplyScore(profileId, delta);
		}
	}
}

public abstract class WdfModeRuntime
{
	internal abstract void ApplyScore(Guid profileId, long scoreDelta);
}

public sealed class WdfBossRuntime(WdfBossDefinition definition) : WdfModeRuntime
{
	private readonly Dictionary<Guid, long> contributions = [];
	public long RemainingHealth { get; private set; } = definition.Health;
	public bool Defeated => RemainingHealth == 0;
	public IReadOnlyDictionary<Guid, long> Contributions => contributions;

	internal override void ApplyScore(Guid profileId, long scoreDelta)
	{
		contributions[profileId] = contributions.GetValueOrDefault(profileId) + scoreDelta;
		RemainingHealth = Math.Max(0, RemainingHealth - scoreDelta);
	}
}

public sealed class WdfTeamBattleRuntime(WdfTeamDefinition[] teams) : WdfModeRuntime
{
	private readonly Dictionary<Guid, string> assignments = [];
	private readonly Dictionary<string, long> scores = teams.ToDictionary(value => value.Id, _ => 0L, StringComparer.OrdinalIgnoreCase);

	public IReadOnlyDictionary<Guid, string> Assignments => assignments;
	public IReadOnlyDictionary<string, long> Scores => scores;

	internal override void ApplyScore(Guid profileId, long scoreDelta)
	{
		if (!assignments.TryGetValue(profileId, out string? teamId))
		{
			teamId = teams.OrderBy(team => assignments.Values.Count(value =>
				value.Equals(team.Id, StringComparison.OrdinalIgnoreCase))).First().Id;
			assignments[profileId] = teamId;
		}

		scores[teamId] += scoreDelta;
	}
}

public sealed class WdfTournamentRuntime(WdfTournamentDefinition definition) : WdfModeRuntime
{
	private readonly Dictionary<Guid, long> scores = [];
	public int MaximumParticipants => definition.MaximumParticipants;
	public IReadOnlyDictionary<Guid, long> Scores => scores;

	internal override void ApplyScore(Guid profileId, long scoreDelta)
	{
		if (!scores.ContainsKey(profileId) && scores.Count >= MaximumParticipants)
			return;
		scores[profileId] = scores.GetValueOrDefault(profileId) + scoreDelta;
	}
}
