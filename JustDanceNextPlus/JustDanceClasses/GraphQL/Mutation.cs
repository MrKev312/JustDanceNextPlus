using HotChocolate;

using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.JustDanceClasses.GraphQL.Objects;
using JustDanceNextPlus.Services;

namespace JustDanceNextPlus.JustDanceClasses.GraphQL;

public class Mutation
{
	public async Task<ExecutionResult<MapStatsResponse>> PushMapsPlayedAsync(
		PushMapsPlayedInput input,
		[Service] UserDataService userDataService,
		[Service] SessionManager sessionManager,
		[Service] IHttpContextAccessor httpContextAccessor)
	{
		// Method to create a standardized error response
		ExecutionResult<MapStatsResponse> error = new() { Success = false };

		// From the header, get the session id
		string? sessionId = httpContextAccessor.HttpContext?.Request.Headers["ubi-sessionid"].FirstOrDefault();

		// If the input is null, return an error response
		if (input == null || sessionId == null || input.Maps.Score is < 0 or > 13333 ||
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
		MapInfo map = input.Maps;

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
				Gold = map.GoldMovesCount
			}
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
		return new ExecutionResult<MapStatsResponse>
		{
			Success = true,
			Response = new MapStatsResponse
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

	public async Task<ExecutionResult<PlaylistStatsResponse>> PushPlaylistPlayedAsync(
		PushPlaylistPlayedInput input,
		[Service] UserDataService userDataService,
		[Service] SessionManager sessionManager,
		[Service] IHttpContextAccessor httpContextAccessor)
	{
		// Method to create a standardized error response
		ExecutionResult<PlaylistStatsResponse> error = new() { Success = false };

		// From the header, get the session id
		string? sessionId = httpContextAccessor.HttpContext?.Request.Headers["ubi-sessionid"].FirstOrDefault();
		// If the input is null, return an error response
		if (input == null || sessionId == null 
			|| input.Playlist.TotalScore is < 0 || input.Playlist.TotalScore > (input.Playlist.MapData.Count * 13333)
			|| input.Playlist.MapData.Count == 0 || input.Playlist.MapData.Any(m => m.Score is < 0 or > 13333)
			|| !Guid.TryParse(sessionId, out Guid sessionGuid))
			return error;

		// Get the profile id from the session id
		Session? session = sessionManager.GetSessionById(sessionGuid);
		if (session == null)
			return error;

		// Get the profile from the profile id
		Profile? profile = await userDataService.GetProfileByIdAsync(session.PlayerId);
		if (profile == null)
			return error;

		// Get the playlist with the highest score
		PlaylistInfo playlist = input.Playlist;

		// Create a high score entry for the playlist
		PlaylistStats playlistStats = new()
		{
			PlayCount = 1,
			HighScore = playlist.TotalScore,
			PlaylistId = playlist.PlaylistId,
			ProfileId = profile.Id,
			HighScorePerMap = playlist.MapData
				.ToDictionary(m => m.MapId, m => m.Score)
		};

		// Update the high score
		(bool success, bool isNewHighScore) = await userDataService.UpdatePlaylistHighScoreAsync(playlistStats, playlist.TotalScore, profile.Id, playlist.PlaylistId);

		// Get the high score for the playlist
		PlaylistStats? playlistHighScore = await userDataService.GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync(profile.Id, playlist.PlaylistId);

		if (!success)
			return error;

		playlistHighScore ??= new();

		return new ExecutionResult<PlaylistStatsResponse>
		{
			Success = true,
			Response = new PlaylistStatsResponse
			{
				IsHighScore = isNewHighScore,
				PlaylistStats = new PlaylistStatsDataResponse
				{
					HighScore = playlistHighScore.HighScore,
					PlayCount = playlistHighScore.PlayCount,
					HighScorePerMap = [.. playlistHighScore.HighScorePerMap.Select(m => new MapDataResponse
					{
						MapId = m.Key,
						HighScore = m.Value
					})],
					Platform = playlistHighScore.Platform
				},
			}
		};
	}
}
