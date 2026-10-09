using Microsoft.Data.Sqlite;

using System.Text.Json;

namespace JustDanceServer.Services;

public static class MapIdentityMigrationFunctions
{
	public static void Register(
		SqliteConnection connection,
		IReadOnlyDictionary<Guid, string> mapNames,
		Action<string>? warn = null)
	{
		IReadOnlyDictionary<string, Guid> songIds = mapNames
			.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);
		HashSet<Guid> warnedSongIds = [];

		string? ResolveMapName(Guid songId)
		{
			if (mapNames.TryGetValue(songId, out string? mapName))
				return mapName;

			if (warnedSongIds.Add(songId))
			{
				warn?.Invoke(
					$"Dropping legacy score for JDNext song ID '{songId}' because no exact " +
					"loaded map match exists.");
			}

			return null;
		}

		connection.CreateFunction<string, string?>("jd_map_name", value =>
		{
			if (!Guid.TryParse(value, out Guid songId))
				throw new InvalidDataException($"Invalid JDNext song ID '{value}' in the score database.");

			return ResolveMapName(songId);
		});

		connection.CreateFunction<string, string>("jd_map_score_names", value =>
		{
			Dictionary<Guid, int> legacyScores =
				JsonSerializer.Deserialize<Dictionary<Guid, int>>(value) ?? [];
			Dictionary<string, int> migratedScores = [with(StringComparer.OrdinalIgnoreCase)];

			foreach ((Guid songId, int score) in legacyScores)
			{
				string? mapName = ResolveMapName(songId);
				if (mapName is null)
					continue;

				migratedScores[mapName] = migratedScores.TryGetValue(mapName, out int existing)
					? Math.Max(existing, score)
					: score;
			}

			return JsonSerializer.Serialize(migratedScores);
		});

		connection.CreateFunction<string, string>("jd_song_id", value =>
		{
			if (!songIds.TryGetValue(value, out Guid songId))
			{
				throw new InvalidOperationException(
					$"Cannot roll back score for unknown map name '{value}'. " +
					"Restore that map before rolling back the database.");
			}

			return songId.ToString();
		});

		connection.CreateFunction<string, string>("jd_map_score_ids", value =>
		{
			Dictionary<string, int> currentScores =
				JsonSerializer.Deserialize<Dictionary<string, int>>(value) ?? [];
			Dictionary<Guid, int> legacyScores = [];

			foreach ((string mapName, int score) in currentScores)
			{
				if (!songIds.TryGetValue(mapName, out Guid songId))
				{
					throw new InvalidOperationException(
						$"Cannot roll back playlist score for unknown map name '{mapName}'. " +
						"Restore that map before rolling back the database.");
				}

				legacyScores[songId] = score;
			}

			return JsonSerializer.Serialize(legacyScores);
		});
	}
}
