using HotChocolate;

using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;
using JustDanceNextPlus.Services;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL;

public class Mutation
{
	public async Task<MapScoreResponse> PushMapsPlayedAsync(
		PushMapsPlayedInput input,
		[Service] UserDataService userDataService,
		[Service] SessionManager sessionManager,
		[Service] IHttpContextAccessor httpContextAccessor)
	{
		// Method to create a standardized error response
		MapScoreResponse error = new() { Success = false };

		// From the header, get the session id
		string? sessionId = httpContextAccessor.HttpContext?.Request.Headers["ubi-sessionid"].FirstOrDefault();

		// If the input is null, return an error response
		if (input == null || sessionId == null || input.Maps.Length == 0 || input.Maps.Any(x => x.Score is < 0 or > 13333) ||
			!Guid.TryParse(sessionId, out Guid sessionGuid))
			return error;

		// Get the profile id from the session id
		Session? session = sessionManager.GetSessionById(sessionGuid);

		if (session == null)
			return error;

		// Get the profile from the profile id
		Profile? profile = await userDataService.GetProfileByIdAsync(session.PlayerId);

		if (profile == null)
			return error;

		// Get the map with the highest score
		MapInfo map = input.Maps.OrderByDescending(x => x.Score).First();

		// Create a high score entry for the map
		HighscorePerformance highScorePerformance = new()
		{
			GoldMovesAchieved = map.GoldMovesAchieved,
			Moves = new MoveCounts
			{
				Missed = map.MissedMovesCount,
				Okay = map.OkayMovesCount,
				Good = map.GoodMovesCount,
				Super = map.SuperMovesCount,
				Perfect = map.PerfectMovesCount,
				Gold = map.GoldMovesCount,

				ProfileId = profile.Id,
				MapId = map.MapId
			},

			ProfileId = profile.Id,
			MapId = map.MapId
		};

		// Update the high score
		(bool success, bool isNewHighScore) = await userDataService.UpdateHighScoreAsync(highScorePerformance, map.Score, profile.Id, map.MapId);

		// Get the high score for the map
		MapStats? highScoreForMap = await userDataService.GetHighScoreByProfileIdAndMapIdAsync(profile.Id, map.MapId);

		if (!success)
			return error;

		highScoreForMap ??= new()
		{
			HighScore = map.Score,
			PlayCount = 1,
			HighScorePerformance = highScorePerformance,
			ProfileId = profile.Id,
			MapId = map.MapId
		};

		// Business logic to process input and generate a response
		return new MapScoreResponse
		{
			Success = true,
			Response = new ResponseData
			{
				EarnedXP = 0,
				IsHighScore = isNewHighScore,
				CurrentXP = profile.CurrentXP,
				CurrentLevel = profile.CurrentLevel,
				IsLevelIncreased = false,
				IsPrestigeIncreased = false,
				PrestigeGrade = 0,
				MapStats = new MapStats
				{
					HighScore = highScoreForMap.HighScore,
					PlayCount = highScoreForMap.PlayCount,
					HighScorePerformance = highScoreForMap.HighScorePerformance,
					GameModeStats = null,
					ProfileId = profile.Id,
					MapId = map.MapId
				},
				EarnedSeasonPoints = 0
			}
		};
	}
}
