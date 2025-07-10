using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.EntityFrameworkCore;

namespace JustDanceNextPlus.Services;

public class UserDataService(
	UserDataContext dbContext,
	ILogger<UserDataService> logger)
{
	public async ValueTask<Profile?> GetProfileByIdAsync(Guid id)
	{
		Profile? profile = await dbContext.Profiles.FindAsync(id);

		if (profile == null)
			return null;

		// Now we populate the profile with the rest of the data
		profile.MapStats = await dbContext.HighScores
			.Where(hs => hs.ProfileId == id)
			.ToDictionaryAsync(hs => hs.MapId);

		profile.PlaylistStats = await dbContext.PlaylistHighScores
			.Where(ph => ph.ProfileId == id)
			.ToDictionaryAsync(ph => ph.PlaylistId);

		return profile;
	}

	public async ValueTask<Profile?> GetProfileByTicketAsync(string ticket)
	{
		return await dbContext.Profiles.FirstOrDefaultAsync(p => p.Ticket == ticket);
	}

	public async ValueTask<bool> HasJustDanceProfileAsync(Guid id)
	{
		return await dbContext.Profiles.AnyAsync(p => p.Id == id);
	}

	public async ValueTask<Guid> GenerateAddProfileAsync(Profile profile)
	{
		Guid id = profile.Id;

		while (id == Guid.Empty || await HasJustDanceProfileAsync(id))
			id = Guid.NewGuid();

		profile.Id = id;
		profile.Dancercard.Id = id;

		await AddProfileAsync(profile);

		return id;
	}

	public async ValueTask<bool> AddProfileAsync(Profile profile)
	{
		if (profile.Id == Guid.Empty || profile.Dancercard.Id == Guid.Empty)
			return false;

		if (await HasJustDanceProfileAsync(profile.Id))
			return false;

		dbContext.Profiles.Add(profile);
		await dbContext.SaveChangesAsync();
		return true;
	}

	public async ValueTask<bool> UpdateProfileAsync(Profile profile)
	{
		dbContext.Profiles.Update(profile);
		try
		{
			await dbContext.SaveChangesAsync();
			return true;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to update profile");
			return false;
		}
	}

	// Map leaderboard
	public async ValueTask<Leaderboard> GetLeaderboardByMapIdAsync(Guid mapId, long limit, long offset)
	{
		List<MapStats> highScores = await dbContext.HighScores
			.Where(hs => hs.MapId == mapId)
			.Join(dbContext.Profiles,
				hs => hs.ProfileId,
				p => p.Id,
				(hs, p) => new { HighScore = hs, ProfileName = p.Dancercard.Name })
			.OrderByDescending(hs => hs.HighScore.HighScore)
			.ThenBy(hs => hs.ProfileName)
			.Skip((int)offset)
			.Take((int)limit)
			.Select(hs => hs.HighScore)
			.ToListAsync();

		Leaderboard leaderboard = new()
		{
			Count = highScores.Count
		};

		foreach (MapStats highScore in highScores)
		{
			long position = await dbContext.HighScores
				.Where(hs => hs.MapId == mapId && hs.HighScore > highScore.HighScore)
				.CountAsync() + 1;

			ScoreDetails newScore = new()
			{
				ProfileId = highScore.ProfileId,
				Score = highScore.HighScore,
				Position = position
			};

			leaderboard.Scores.Add(newScore);
		}

		return leaderboard;
	}

	public async ValueTask<Leaderboard> GetLeaderboardAroundAsync(Guid mapId, Guid userId, int limit = 3)
	{
		// Pre-fetch profiles and their dancer card names in one query to avoid multiple async calls
		List<MapStats> highScoresWithNames = await dbContext.HighScores
			.Where(hs => hs.MapId == mapId)
			.Join(dbContext.Profiles,
				hs => hs.ProfileId,
				p => p.Id,
				(hs, p) => new
				{
					hs.ProfileId,
					hs.HighScore,
					DancerCardName = p.Dancercard.Name
				})
			.OrderByDescending(hs => hs.HighScore)
			.ThenBy(hs => hs.DancerCardName)
			.Select(hs => new MapStats
			{
				ProfileId = hs.ProfileId,
				HighScore = hs.HighScore
			})
			.ToListAsync();

		// Find the user's index
		int userIndex = highScoresWithNames.FindIndex(hs => hs.ProfileId == userId);

		// Get the surrounding scores
		List<MapStats> surroundingScores = [.. highScoresWithNames
			.Skip(Math.Max(0, userIndex - (limit / 2)))
			.Take(limit)];

		Leaderboard leaderboard = new()
		{
			Count = surroundingScores.Count
		};

		foreach (MapStats highScore in surroundingScores)
		{
			long position = await dbContext.HighScores
				.Where(hs => hs.MapId == mapId && hs.HighScore > highScore.HighScore)
				.CountAsync() + 1;

			ScoreDetails newScore = new()
			{
				ProfileId = highScore.ProfileId,
				Score = highScore.HighScore,
				Position = position
			};

			leaderboard.Scores.Add(newScore);
		}

		return leaderboard;
	}

	public async ValueTask<Leaderboard> GetLeaderboardFromIdsAsync(Guid mapId, List<Guid> userIds)
	{
		List<MapStats> highScores = await dbContext.HighScores
			.Where(hs => hs.MapId == mapId && userIds.Contains(hs.ProfileId))
			.OrderByDescending(hs => hs.HighScore)
			.ToListAsync();

		Leaderboard leaderboard = new()
		{
			Count = highScores.Count
		};

		foreach (MapStats highScore in highScores)
		{
			long position = await dbContext.HighScores
				.Where(hs => hs.MapId == mapId && hs.HighScore > highScore.HighScore)
				.CountAsync() + 1;

			ScoreDetails newScore = new()
			{
				ProfileId = highScore.ProfileId,
				Score = highScore.HighScore,
				Position = position
			};

			leaderboard.Scores.Add(newScore);
		}

		return leaderboard;
	}

	public async ValueTask<Leaderboard> GetRandomOpponentAsync(Guid mapId, Guid playerId)
	{
		// Get a random high score
		MapStats? randomHighScore = dbContext.HighScores
			.Where(hs => hs.MapId == mapId && hs.ProfileId != playerId)
			.AsEnumerable()
			.OrderBy(hs => Guid.NewGuid())
			.FirstOrDefault();

		randomHighScore ??= await dbContext.HighScores
			.Where(hs => hs.MapId == mapId)
			.FirstOrDefaultAsync();

		if (randomHighScore == null)
			return new Leaderboard();
			
		Leaderboard leaderboard = new()
		{
			Count = 1
		};

		long position = await dbContext.HighScores
			.Where(hs => hs.MapId == mapId && hs.HighScore > randomHighScore.HighScore)
			.CountAsync() + 1;

		ScoreDetails newScore = new()
		{
			ProfileId = randomHighScore.ProfileId,
			Score = randomHighScore.HighScore,
			Position = position
		};

		leaderboard.Scores.Add(newScore);

		return leaderboard;
	}

	// Playlist leaderboard
	public async ValueTask<Leaderboard> GetPlaylistLeaderboardByPlaylistIdAsync(Guid playlistId, long limit, long offset)
	{
		List<PlaylistStats> highScores = await dbContext.PlaylistHighScores
			.Where(ph => ph.PlaylistId == playlistId)
			.Join(dbContext.Profiles,
				ph => ph.ProfileId,
				p => p.Id,
				(ph, p) => new { HighScore = ph, ProfileName = p.Dancercard.Name })
			.OrderByDescending(ph => ph.HighScore.HighScore)
			.ThenBy(ph => ph.ProfileName)
			.Skip((int)offset)
			.Take((int)limit)
			.Select(ph => ph.HighScore)
			.ToListAsync();

		Leaderboard leaderboard = new()
		{
			Count = highScores.Count
		};

		foreach (PlaylistStats highScore in highScores)
		{
			long position = await dbContext.PlaylistHighScores
				.Where(ph => ph.PlaylistId == playlistId && ph.HighScore > highScore.HighScore)
				.CountAsync() + 1;

			ScoreDetails newScore = new()
			{
				ProfileId = highScore.ProfileId,
				Score = highScore.HighScore,
				Position = position
			};
			leaderboard.Scores.Add(newScore);
		}

		return leaderboard;
	}

	public async ValueTask<Leaderboard> GetPlaylistLeaderboardAroundAsync(Guid playlistId, Guid userId, int limit = 3)
	{
		// Pre-fetch profiles and their dancer card names in one query to avoid multiple async calls
		List<PlaylistStats> highScoresWithNames = await dbContext.PlaylistHighScores
			.Where(ph => ph.PlaylistId == playlistId)
			.Join(dbContext.Profiles,
				ph => ph.ProfileId,
				p => p.Id,
				(ph, p) => new
				{
					ph.ProfileId,
					ph.HighScore,
					DancerCardName = p.Dancercard.Name
				})
			.OrderByDescending(ph => ph.HighScore)
			.ThenBy(ph => ph.DancerCardName)
			.Select(ph => new PlaylistStats
			{
				ProfileId = ph.ProfileId,
				HighScore = ph.HighScore
			})
			.ToListAsync();

		int userIndex = highScoresWithNames.FindIndex(ph => ph.ProfileId == userId);

		List<PlaylistStats> surroundingScores = [.. highScoresWithNames
			.Skip(Math.Max(0, userIndex - (limit / 2)))
			.Take(limit)];

		Leaderboard leaderboard = new()
		{
			Count = surroundingScores.Count
		};

		foreach (PlaylistStats highScore in surroundingScores)
		{
			long position = await dbContext.PlaylistHighScores
				.Where(ph => ph.PlaylistId == playlistId && ph.HighScore > highScore.HighScore)
				.CountAsync() + 1;
			ScoreDetails newScore = new()
			{
				ProfileId = highScore.ProfileId,
				Score = highScore.HighScore,
				Position = position
			};
			leaderboard.Scores.Add(newScore);
		}

		return leaderboard;
	}

	public async ValueTask<Leaderboard> GetPlaylistLeaderboardFromIdsAsync(Guid playlistId, List<Guid> userIds)
	{
		List<PlaylistStats> highScores = await dbContext.PlaylistHighScores
			.Where(ph => ph.PlaylistId == playlistId && userIds.Contains(ph.ProfileId))
			.OrderByDescending(ph => ph.HighScore)
			.ToListAsync();

		Leaderboard leaderboard = new()
		{
			Count = highScores.Count
		};

		foreach (PlaylistStats highScore in highScores)
		{
			long position = await dbContext.PlaylistHighScores
				.Where(ph => ph.PlaylistId == playlistId && ph.HighScore > highScore.HighScore)
				.CountAsync() + 1;
			ScoreDetails newScore = new()
			{
				ProfileId = highScore.ProfileId,
				Score = highScore.HighScore,
				Position = position
			};
			leaderboard.Scores.Add(newScore);
		}

		return leaderboard;
	}

	// Map scores
	public async ValueTask<List<MapStats>> GetHighScoresByProfileIdAsync(Guid profileId)
	{
		return await dbContext.HighScores
			.Where(hs => hs.ProfileId == profileId)
			.ToListAsync();
	}

	public async ValueTask<MapStats?> GetHighScoreByProfileIdAndMapIdAsync(Guid profileId, Guid mapId)
	{
		return await dbContext.HighScores
			.FirstOrDefaultAsync(hs => hs.ProfileId == profileId && hs.MapId == mapId);
	}

	public async ValueTask<(bool Success, bool IsNewHighScore)> UpdateHighScoreAsync(HighscorePerformance highScore, int score, Guid playerId, Guid mapId)
	{
		if (playerId == Guid.Empty || mapId == Guid.Empty)
			return (false, false);

		// If the score is 0, just return
		if (score == 0)
			return (true, false);

		MapStats? existingHighScore = await dbContext.HighScores
			.FirstOrDefaultAsync(hs => hs.ProfileId == playerId && hs.MapId == mapId);

		bool isNewHighScore = false;

		if (existingHighScore == null)
		{
			MapStats newHighScore = new()
			{
				ProfileId = playerId,
				MapId = mapId,
				UpdatedAt = DateTime.UtcNow,
				HighScore = score,
				PlayCount = 1,
				HighScorePerformance = highScore
			};

			dbContext.HighScores.Add(newHighScore);
			isNewHighScore = true;
		}
		else
		{
			if (score > existingHighScore.HighScore)
			{
				existingHighScore.UpdatedAt = DateTime.UtcNow;
				existingHighScore.HighScore = score;
				existingHighScore.HighScorePerformance = highScore;

				isNewHighScore = true;
			}

			existingHighScore.PlayCount++;

			dbContext.HighScores.Update(existingHighScore);
		}

		try
		{
			await dbContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to update high score");
			return (false, false);
		}

		return (true, isNewHighScore);
	}

	// Playlist scores
	public async ValueTask<List<PlaylistStats>> GetPlaylistHighScoresByProfileIdAsync(Guid profileId)
	{
		return await dbContext.PlaylistHighScores
			.Where(ph => ph.ProfileId == profileId)
			.ToListAsync();
	}

	public async ValueTask<PlaylistStats?> GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync(Guid profileId, Guid playlistId)
	{
		return await dbContext.PlaylistHighScores
			.FirstOrDefaultAsync(ph => ph.ProfileId == profileId && ph.PlaylistId == playlistId);
	}

	public async ValueTask<(bool Success, bool IsNewHighScore)> UpdatePlaylistHighScoreAsync(PlaylistStats playlistStats, int score, Guid playerId, Guid playlistId)
	{
		if (playerId == Guid.Empty || playlistId == Guid.Empty)
			return (false, false);

		// If the score is 0, just return
		if (score == 0)
			return (true, false);

		PlaylistStats? existingHighScore = await dbContext.PlaylistHighScores
			.FirstOrDefaultAsync(ph => ph.ProfileId == playerId && ph.PlaylistId == playlistId);

		bool isNewHighScore = false;

		if (existingHighScore == null)
		{
			PlaylistStats newHighScore = new()
			{
				ProfileId = playerId,
				PlaylistId = playlistId,
				UpdatedAt = DateTime.UtcNow,
				HighScore = score,
				PlayCount = 1,
				HighScorePerMap = playlistStats.HighScorePerMap
			};
			dbContext.PlaylistHighScores.Add(newHighScore);
			isNewHighScore = true;
		}
		else
		{
			if (score > existingHighScore.HighScore)
			{
				existingHighScore.UpdatedAt = DateTime.UtcNow;
				existingHighScore.HighScore = score;
				existingHighScore.HighScorePerMap = playlistStats.HighScorePerMap;
				isNewHighScore = true;
			}

			existingHighScore.PlayCount++;
			dbContext.PlaylistHighScores.Update(existingHighScore);
		}

		try
		{
			await dbContext.SaveChangesAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to update playlist high score");
			return (false, false);
		}

		return (true, isNewHighScore);
	}
}
