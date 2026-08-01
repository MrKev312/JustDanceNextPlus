using HotChocolate;

using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.GraphQL.Objects;
using JustDanceServer.Services;

namespace JustDanceServer.JustDanceClasses.GraphQL;

public class Mutation
{
	[GraphQLName("createProfile")]
	public async Task<CreateProfileResponse> CreateProfileAsync(
		CreateProfileInput input,
		[Service] IUserDataService userDataService,
		[Service] ISessionManager sessionManager,
		[Service] IHttpContextAccessor httpContextAccessor)
	{
		Profile? profile = await GetSessionProfileAsync(userDataService, sessionManager, httpContextAccessor);
		if (profile == null)
			return new CreateProfileResponse { Success = false };

		profile.Dancercard = input.ToDancerCard(profile.Id, profile.Dancercard);
		bool updated = await userDataService.UpdateProfileAsync(profile);

		return new CreateProfileResponse { Success = updated };
	}

	public async Task<ExecutionResult<MapStatsResponse>> PushMapsPlayedAsync(
		PushMapsPlayedInput input,
		[Service] IUserDataService userDataService,
		[Service] ISessionManager sessionManager,
		[Service] IHttpContextAccessor httpContextAccessor)
	{
		ExecutionResult<MapStatsResponse> error = new() { Success = false };

		Profile? profile = await GetSessionProfileAsync(userDataService, sessionManager, httpContextAccessor);
		if (profile == null || input.Maps.Score is < 0 or > 13333)
			return error;

		MapInfo map = input.Maps;
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

		(bool success, bool isNewHighScore) = await userDataService.UpdateHighScoreAsync(highScorePerformance, map.Score, profile.Id, map.MapId);
		if (!success)
			return error;

		MapStats? highScoreForMap = await userDataService.GetHighScoreByProfileIdAndMapIdAsync(profile.Id, map.MapId);
		highScoreForMap ??= new MapStats
		{
			HighScore = map.Score,
			PlayCount = 1,
			HighScorePerformance = highScorePerformance,
			ProfileId = profile.Id,
			MapId = map.MapId
		};

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
				PrestigeGrade = profile.PrestigeGrade,
				MapStats = highScoreForMap,
				EarnedSeasonPoints = 0
			}
		};
	}

	public async Task<ExecutionResult<PlaylistStatsResponse>> PushPlaylistPlayedAsync(
		PushPlaylistPlayedInput input,
		[Service] IUserDataService userDataService,
		[Service] ISessionManager sessionManager,
		[Service] IHttpContextAccessor httpContextAccessor)
	{
		ExecutionResult<PlaylistStatsResponse> error = new() { Success = false };

		Profile? profile = await GetSessionProfileAsync(userDataService, sessionManager, httpContextAccessor);
		if (profile == null
			|| input.Playlist.TotalScore < 0
			|| input.Playlist.MapData.Count == 0
			|| input.Playlist.TotalScore > input.Playlist.MapData.Count * 13333
			|| input.Playlist.MapData.Any(map => map.Score is < 0 or > 13333))
			return error;

		PlaylistInfo playlist = input.Playlist;
		PlaylistStats playlistStats = new()
		{
			PlayCount = 1,
			HighScore = playlist.TotalScore,
			PlaylistId = playlist.PlaylistId,
			ProfileId = profile.Id,
			HighScorePerMap = playlist.MapData.ToDictionary(map => map.MapId, map => map.Score)
		};

		(bool success, bool isNewHighScore) = await userDataService.UpdatePlaylistHighScoreAsync(playlistStats, playlist.TotalScore, profile.Id, playlist.PlaylistId);
		if (!success)
			return error;

		PlaylistStats? playlistHighScore = await userDataService.GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync(profile.Id, playlist.PlaylistId);
		playlistHighScore ??= playlistStats;

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
					HighScorePerMap = [.. playlistHighScore.HighScorePerMap.Select(map => new MapDataResponse
					{
						MapId = map.Key,
						HighScore = map.Value
					})],
					Platform = playlistHighScore.Platform
				}
			}
		};
	}

	public async Task<UpdateDancercardResponse> UpdateDancercardAsync(
		UpdateDancercardInput input,
		[Service] IUserDataService userDataService,
		[Service] ISessionManager sessionManager,
		[Service] IHttpContextAccessor httpContextAccessor)
	{
		Profile? profile = await GetSessionProfileAsync(userDataService, sessionManager, httpContextAccessor);
		if (profile == null)
			return new UpdateDancercardResponse { Success = false };

		profile.Dancercard = input.ToDancerCard(profile.Id, profile.Dancercard);
		bool updated = await userDataService.UpdateProfileAsync(profile);

		return new UpdateDancercardResponse
		{
			Success = updated,
			Dancercard = updated ? profile.Dancercard : null
		};
	}

	private static async Task<Profile?> GetSessionProfileAsync(
		IUserDataService userDataService,
		ISessionManager sessionManager,
		IHttpContextAccessor httpContextAccessor)
	{
		string? sessionId = httpContextAccessor.HttpContext?.Request.Headers["ubi-sessionid"].FirstOrDefault();
		if (!Guid.TryParse(sessionId, out Guid sessionGuid))
			return null;

		Session? session = sessionManager.GetSessionById(sessionGuid);
		return session == null ? null : await userDataService.GetProfileByIdAsync(session.PlayerId);
	}
}
