using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;

namespace JustDanceServer.Services;

public interface IPlayerPreferenceService
{
	ValueTask<string[]> GetFavoritesAsync(Guid profileId);
	ValueTask<bool> SetFavoriteAsync(Guid profileId, string mapName, bool favorite);
}

public sealed class PlayerPreferenceService(
	UserDataContext dbContext,
	IMapService mapService,
	TimeProvider timeProvider) : IPlayerPreferenceService
{
	public async ValueTask<string[]> GetFavoritesAsync(Guid profileId)
		=> await dbContext.PlayerMapFavorites.AsNoTracking()
			.Where(value => value.ProfileId == profileId)
			.OrderByDescending(value => value.FavoritedAtUtc)
			.ThenBy(value => value.MapName)
			.Select(value => value.MapName)
			.ToArrayAsync();

	public async ValueTask<bool> SetFavoriteAsync(Guid profileId, string mapName, bool favorite)
	{
		ServerMap? map = mapService.GetMap(mapName);
		if (profileId == Guid.Empty || map is null
			|| !await dbContext.Profiles.AnyAsync(value => value.Id == profileId))
			return false;

		string canonicalName = map.Song.MapName;
		DateTime now = timeProvider.GetUtcNow().UtcDateTime;
		if (dbContext.Database.IsSqlite())
		{
			if (favorite)
			{
				await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
					INSERT INTO "PlayerMapFavorites" ("ProfileId", "MapName", "FavoritedAtUtc")
					VALUES ({profileId}, {canonicalName}, {now})
					ON CONFLICT ("ProfileId", "MapName") DO NOTHING;
					""");
			}
			else
			{
				await dbContext.PlayerMapFavorites
					.Where(value => value.ProfileId == profileId && value.MapName == canonicalName)
					.ExecuteDeleteAsync();
			}

			return true;
		}

		PlayerMapFavorite? existing = await dbContext.PlayerMapFavorites.FindAsync(profileId, canonicalName);
		if (favorite && existing is null)
			dbContext.PlayerMapFavorites.Add(new PlayerMapFavorite
			{
				ProfileId = profileId,
				MapName = canonicalName,
				FavoritedAtUtc = now
			});
		else if (!favorite && existing is not null)
			dbContext.PlayerMapFavorites.Remove(existing);
		await dbContext.SaveChangesAsync();
		return true;
	}
}
