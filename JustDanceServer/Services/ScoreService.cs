using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using System.Data;
using System.Text.Json;

namespace JustDanceServer.Services;

public sealed class ScoreService(
	UserDataContext dbContext,
	ILogger<ScoreService> logger,
	TimeProvider? timeProvider = null) : IScoreService
{
	private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

	public async ValueTask<List<MapStats>> GetHighScoresByProfileIdAsync(Guid profileId)
		=> await dbContext.MapScores.AsNoTracking()
			.Where(score => score.ProfileId == profileId)
			.ToListAsync();

	public async ValueTask<MapStats?> GetHighScoreByProfileIdAndMapNameAsync(Guid profileId, string mapName)
		=> await dbContext.MapScores.AsNoTracking().FirstOrDefaultAsync(
			score => score.ProfileId == profileId && score.MapName == mapName);

	public async ValueTask<List<PlaylistStats>> GetPlaylistHighScoresByProfileIdAsync(Guid profileId)
		=> await dbContext.PlaylistScores.AsNoTracking()
			.Where(score => score.ProfileId == profileId)
			.ToListAsync();

	public async ValueTask<PlaylistStats?> GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync(
		Guid profileId,
		Guid playlistId)
		=> await dbContext.PlaylistScores.AsNoTracking().FirstOrDefaultAsync(
			score => score.ProfileId == profileId && score.PlaylistId == playlistId);

	public ValueTask<ScoreUpdateResult> RecordMapPlayAsync(
		HighscorePerformance highScorePerformance,
		int score,
		Guid playerId,
		string mapName,
		string platform,
		bool isCoopHighScore = false)
		=> WriteMapScoreAsync(
			highScorePerformance,
			score,
			playerId,
			mapName,
			platform,
			isCoopHighScore,
			playCount: 1,
			mergePlayCount: false);

	public ValueTask<ScoreUpdateResult> MergeMapScoreAsync(
		HighscorePerformance highScorePerformance,
		int score,
		int playCount,
		Guid playerId,
		string mapName,
		string platform,
		bool isCoopHighScore = false)
		=> WriteMapScoreAsync(
			highScorePerformance,
			score,
			playerId,
			mapName,
			platform,
			isCoopHighScore,
			Math.Max(0, playCount),
			mergePlayCount: true);

	public async ValueTask<ScoreUpdateResult> UpdatePlaylistHighScoreAsync(
		PlaylistStats playlistStats,
		int score,
		Guid playerId,
		Guid playlistId)
	{
		if (playerId == Guid.Empty || playlistId == Guid.Empty || score < 0)
			return new(false, false);
		if (score == 0)
			return new(true, false);

		if (dbContext.Database.IsSqlite())
			return await WriteSqlitePlaylistScoreAsync(playlistStats, score, playerId, playlistId);

		return await WriteTrackedPlaylistScoreAsync(playlistStats, score, playerId, playlistId);
	}

	private async ValueTask<ScoreUpdateResult> WriteMapScoreAsync(
		HighscorePerformance highScorePerformance,
		int score,
		Guid playerId,
		string mapName,
		string platform,
		bool isCoopHighScore,
		int playCount,
		bool mergePlayCount)
	{
		mapName = mapName.Trim();
		if (playerId == Guid.Empty || mapName.Length == 0 || score is < 0 or > 13333)
			return new(false, false);
		if (score == 0)
			return new(true, false);

		DateTime highScoreAchievedAt = timeProvider.GetUtcNow().UtcDateTime;

		if (dbContext.Database.IsSqlite())
		{
			return await WriteSqliteMapScoreAsync(
				highScorePerformance,
				score,
				playerId,
				mapName,
				platform,
				isCoopHighScore,
				playCount,
				mergePlayCount,
				highScoreAchievedAt);
		}

		return await WriteTrackedMapScoreAsync(
			highScorePerformance,
			score,
			playerId,
			mapName,
			platform,
			isCoopHighScore,
			playCount,
			mergePlayCount,
			highScoreAchievedAt);
	}

	private async ValueTask<ScoreUpdateResult> WriteSqliteMapScoreAsync(
		HighscorePerformance highScorePerformance,
		int score,
		Guid playerId,
		string mapName,
		string platform,
		bool isCoopHighScore,
		int playCount,
		bool mergePlayCount,
		DateTime highScoreAchievedAt)
	{
		const string incrementPlayCount = "\"MapScores\".\"PlayCount\" + excluded.\"PlayCount\"";
		const string mergeReportedPlayCount = "MAX(\"MapScores\".\"PlayCount\", excluded.\"PlayCount\")";
		string playCountExpression = mergePlayCount ? mergeReportedPlayCount : incrementPlayCount;
		string updatedAt = highScoreAchievedAt.ToString("O");

		try
		{
			SqliteConnection connection = (SqliteConnection)dbContext.Database.GetDbConnection();
			bool closeConnection = connection.State != ConnectionState.Open;
			if (closeConnection)
				await connection.OpenAsync();

			try
			{
				await using SqliteCommand command = connection.CreateCommand();
				command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
				command.CommandText = $$"""
					INSERT INTO "MapScores"
						("MapName", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt",
						 "IsCoopHighScore", "HighScorePerformance", "GameModeStats")
					VALUES
						($mapName, $profileId, $score, $playCount, $platform, $updatedAt,
						 $isCoopHighScore, $performance, NULL)
					ON CONFLICT("MapName", "ProfileId") DO UPDATE SET
						"PlayCount" = {{playCountExpression}},
						"HighScore" = MAX("MapScores"."HighScore", excluded."HighScore"),
						"Platform" = CASE WHEN excluded."HighScore" > "MapScores"."HighScore"
							THEN excluded."Platform" ELSE "MapScores"."Platform" END,
						"UpdatedAt" = CASE WHEN excluded."HighScore" > "MapScores"."HighScore"
							THEN excluded."UpdatedAt" ELSE "MapScores"."UpdatedAt" END,
						"IsCoopHighScore" = CASE WHEN excluded."HighScore" > "MapScores"."HighScore"
							THEN excluded."IsCoopHighScore" ELSE "MapScores"."IsCoopHighScore" END,
						"HighScorePerformance" = CASE WHEN excluded."HighScore" > "MapScores"."HighScore"
							THEN excluded."HighScorePerformance" ELSE "MapScores"."HighScorePerformance" END
					RETURNING CASE WHEN "UpdatedAt" = $updatedAt THEN 1 ELSE 0 END;
					""";
				command.Parameters.AddWithValue("$mapName", mapName);
				command.Parameters.AddWithValue("$profileId", playerId);
				command.Parameters.AddWithValue("$score", score);
				command.Parameters.AddWithValue("$playCount", playCount);
				command.Parameters.AddWithValue("$platform", NormalizePlatform(platform));
				command.Parameters.AddWithValue("$updatedAt", updatedAt);
				command.Parameters.AddWithValue("$isCoopHighScore", isCoopHighScore);
				command.Parameters.AddWithValue(
					"$performance",
					JsonSerializer.Serialize(highScorePerformance, (JsonSerializerOptions?)null));

				object? result = await command.ExecuteScalarAsync();
				return new(true, Convert.ToInt32(result) == 1);
			}
			finally
			{
				if (closeConnection)
					await connection.CloseAsync();
			}
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed to atomically update score for {MapName}", mapName);
			return new(false, false);
		}
	}

	private async ValueTask<ScoreUpdateResult> WriteTrackedMapScoreAsync(
		HighscorePerformance highScorePerformance,
		int score,
		Guid playerId,
		string mapName,
		string platform,
		bool isCoopHighScore,
		int playCount,
		bool mergePlayCount,
		DateTime highScoreAchievedAt)
	{
		MapStats? existingScore = await dbContext.MapScores.FirstOrDefaultAsync(
			value => value.ProfileId == playerId && value.MapName == mapName);
		bool isNewHighScore = existingScore is null || score > existingScore.HighScore;

		if (existingScore is null)
		{
			dbContext.MapScores.Add(new MapStats
			{
				ProfileId = playerId,
				MapName = mapName,
				UpdatedAt = highScoreAchievedAt,
				HighScore = score,
				PlayCount = playCount,
				Platform = NormalizePlatform(platform),
				IsCoopHighScore = isCoopHighScore,
				HighScorePerformance = highScorePerformance
			});
		}
		else
		{
			existingScore.PlayCount = mergePlayCount
				? Math.Max(existingScore.PlayCount, playCount)
				: existingScore.PlayCount + playCount;
			if (isNewHighScore)
			{
				existingScore.UpdatedAt = highScoreAchievedAt;
				existingScore.HighScore = score;
				existingScore.Platform = NormalizePlatform(platform);
				existingScore.IsCoopHighScore = isCoopHighScore;
				existingScore.HighScorePerformance = highScorePerformance;
			}
		}

		return await SaveTrackedAsync(isNewHighScore, "Failed to update map score");
	}

	private async ValueTask<ScoreUpdateResult> WriteSqlitePlaylistScoreAsync(
		PlaylistStats playlistStats,
		int score,
		Guid playerId,
		Guid playlistId)
	{
		string updatedAt = DateTime.UtcNow.ToString("O");
		try
		{
			SqliteConnection connection = (SqliteConnection)dbContext.Database.GetDbConnection();
			bool closeConnection = connection.State != ConnectionState.Open;
			if (closeConnection)
				await connection.OpenAsync();

			try
			{
				await using SqliteCommand command = connection.CreateCommand();
				command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
				command.CommandText = """
					INSERT INTO "PlaylistScores"
						("PlaylistId", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt", "HighScorePerMap")
					VALUES
						($playlistId, $profileId, $score, 1, $platform, $updatedAt, $highScorePerMap)
					ON CONFLICT("PlaylistId", "ProfileId") DO UPDATE SET
						"PlayCount" = "PlaylistScores"."PlayCount" + 1,
						"HighScore" = MAX("PlaylistScores"."HighScore", excluded."HighScore"),
						"Platform" = CASE WHEN excluded."HighScore" > "PlaylistScores"."HighScore"
							THEN excluded."Platform" ELSE "PlaylistScores"."Platform" END,
						"UpdatedAt" = CASE WHEN excluded."HighScore" > "PlaylistScores"."HighScore"
							THEN excluded."UpdatedAt" ELSE "PlaylistScores"."UpdatedAt" END,
						"HighScorePerMap" = CASE WHEN excluded."HighScore" > "PlaylistScores"."HighScore"
							THEN excluded."HighScorePerMap" ELSE "PlaylistScores"."HighScorePerMap" END
					RETURNING CASE WHEN "UpdatedAt" = $updatedAt THEN 1 ELSE 0 END;
					""";
				command.Parameters.AddWithValue("$playlistId", playlistId);
				command.Parameters.AddWithValue("$profileId", playerId);
				command.Parameters.AddWithValue("$score", score);
				command.Parameters.AddWithValue("$platform", NormalizePlatform(playlistStats.Platform));
				command.Parameters.AddWithValue("$updatedAt", updatedAt);
				command.Parameters.AddWithValue(
					"$highScorePerMap",
					JsonSerializer.Serialize(playlistStats.HighScorePerMap, (JsonSerializerOptions?)null));

				object? result = await command.ExecuteScalarAsync();
				return new(true, Convert.ToInt32(result) == 1);
			}
			finally
			{
				if (closeConnection)
					await connection.CloseAsync();
			}
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "Failed to atomically update playlist score for {PlaylistId}", playlistId);
			return new(false, false);
		}
	}

	private async ValueTask<ScoreUpdateResult> WriteTrackedPlaylistScoreAsync(
		PlaylistStats playlistStats,
		int score,
		Guid playerId,
		Guid playlistId)
	{
		PlaylistStats? existingScore = await dbContext.PlaylistScores.FirstOrDefaultAsync(
			value => value.ProfileId == playerId && value.PlaylistId == playlistId);
		bool isNewHighScore = existingScore is null || score > existingScore.HighScore;

		if (existingScore is null)
		{
			dbContext.PlaylistScores.Add(new PlaylistStats
			{
				ProfileId = playerId,
				PlaylistId = playlistId,
				UpdatedAt = DateTime.UtcNow,
				HighScore = score,
				PlayCount = 1,
				Platform = NormalizePlatform(playlistStats.Platform),
				HighScorePerMap = playlistStats.HighScorePerMap
			});
		}
		else
		{
			if (isNewHighScore)
			{
				existingScore.UpdatedAt = DateTime.UtcNow;
				existingScore.HighScore = score;
				existingScore.Platform = NormalizePlatform(playlistStats.Platform);
				existingScore.HighScorePerMap = playlistStats.HighScorePerMap;
			}

			existingScore.PlayCount++;
		}

		return await SaveTrackedAsync(isNewHighScore, "Failed to update playlist score");
	}

	private async ValueTask<ScoreUpdateResult> SaveTrackedAsync(bool isNewHighScore, string errorMessage)
	{
		try
		{
			await dbContext.SaveChangesAsync();
			return new(true, isNewHighScore);
		}
		catch (Exception exception)
		{
			logger.LogError(exception, "{ErrorMessage}", errorMessage);
			return new(false, false);
		}
	}

	private static string NormalizePlatform(string platform)
		=> string.IsNullOrWhiteSpace(platform) ? "Unknown" : platform.Trim().ToLowerInvariant();
}
