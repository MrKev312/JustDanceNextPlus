using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.JustDanceClasses.Endpoints;

using Microsoft.EntityFrameworkCore;

namespace JustDanceNextPlus.Services;

public class UserDataService(
	UserDataContext dbContext,
	ILogger<UserDataService> logger)
{

	public async Task<Profile?> GetProfileByIdAsync(Guid id)
	{
		Profile? profile = await dbContext.Profiles.FindAsync(id);

		if (profile == null)
			return null;

		// Now we populate the profile with the rest of the data
		profile.MapStats = await dbContext.HighScores
			.Where(hs => hs.ProfileId == id)
			.ToDictionaryAsync(hs => hs.MapId);

		return profile;
	}

	public async Task<Profile?> GetProfileByTicketAsync(string ticket)
	{
		return await dbContext.Profiles.FirstOrDefaultAsync(p => p.Ticket == ticket);
	}

	public async Task<bool> HasJustDanceProfileAsync(Guid id)
	{
		return await dbContext.Profiles.AnyAsync(p => p.Id == id);
	}

	public async Task<Guid> GenerateAddProfileAsync(Profile profile)
	{
		Guid id = profile.Id;

		while (id == Guid.Empty || await HasJustDanceProfileAsync(id))
			id = Guid.NewGuid();

		profile.Id = id;
		profile.Dancercard.Id = id;

		await AddProfileAsync(profile);

		return id;
	}

	public async Task<bool> AddProfileAsync(Profile profile)
	{
		if (profile.Id == Guid.Empty || profile.Dancercard.Id == Guid.Empty)
			return false;

		if (await HasJustDanceProfileAsync(profile.Id))
			return false;

		dbContext.Profiles.Add(profile);
		await dbContext.SaveChangesAsync();
		return true;
	}

	public async Task<Leaderboard> GetLeaderboardByMapIdAsync(Guid mapId, long limit, long offset)
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
				.CountAsync();

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

	public async Task<Leaderboard> GetLeaderboardAroundAsync(Guid mapId, Guid userId)
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
		List<MapStats> surroundingScores = highScoresWithNames
			.Skip(Math.Max(0, userIndex - 1))
			.Take(3)
			.ToList();

		Leaderboard leaderboard = new()
		{
			Count = surroundingScores.Count
		};

		foreach (MapStats highScore in surroundingScores)
		{
			long position = await dbContext.HighScores
				.Where(hs => hs.MapId == mapId && hs.HighScore > highScore.HighScore)
				.CountAsync();

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

	public async Task<Leaderboard> GetLeaderboardFromIdsAsync(Guid mapId, List<Guid> userIds)
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
				.CountAsync();

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

	public async Task<Leaderboard> GetRandomOpponentAsync(Guid mapId, Guid playerId)
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
			.CountAsync();

		ScoreDetails newScore = new()
		{
			ProfileId = randomHighScore.ProfileId,
			Score = randomHighScore.HighScore,
			Position = position
		};

		leaderboard.Scores.Add(newScore);

		return leaderboard;
	}

	public async Task<List<MapStats>> GetHighScoresByProfileIdAsync(Guid profileId)
	{
		return await dbContext.HighScores
			.Where(hs => hs.ProfileId == profileId)
			.ToListAsync();
	}

	public async Task<MapStats?> GetHighScoreByProfileIdAndMapIdAsync(Guid profileId, Guid mapId)
	{
		return await dbContext.HighScores
			.FirstOrDefaultAsync(hs => hs.ProfileId == profileId && hs.MapId == mapId);
	}

	public async Task<(bool Success, bool IsNewHighScore)> UpdateHighScoreAsync(HighscorePerformance highScore, int score, Guid playerId, Guid mapId)
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
}
