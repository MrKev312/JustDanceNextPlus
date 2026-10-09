using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;

namespace JustDanceServer.Services;

public interface IPlayerMetadataService
{
	ValueTask<PlayerMetadata> GetAsync(Guid profileId);
	ValueTask<PlayerMetadata> UpdateAsync(
		Guid profileId,
		string? countryCode = null,
		int? countryId = null,
		string? locale = null);
}

public sealed class PlayerMetadataService(UserDataContext dbContext) : IPlayerMetadataService
{
	public async ValueTask<PlayerMetadata> GetAsync(Guid profileId)
	{
		PlayerMetadata? metadata = await dbContext.PlayerMetadata.AsNoTracking()
			.FirstOrDefaultAsync(value => value.ProfileId == profileId);
		if (metadata is not null)
			return metadata;

		Profile? profile = await dbContext.Profiles.AsNoTracking()
			.Include(value => value.Dancercard)
			.FirstOrDefaultAsync(value => value.Id == profileId)
			?? throw new InvalidOperationException($"Profile '{profileId}' does not exist.");
		ClassicProfile? classic = await dbContext.ClassicProfiles.AsNoTracking()
			.FirstOrDefaultAsync(value => value.ProfileId == profileId);
		return new PlayerMetadata
		{
			ProfileId = profileId,
			CountryCode = NormalizeCountryCode(profile.Dancercard.Country),
			CountryId = classic?.CountryId ?? 0
		};
	}

	public async ValueTask<PlayerMetadata> UpdateAsync(
		Guid profileId,
		string? countryCode = null,
		int? countryId = null,
		string? locale = null)
	{
		PlayerMetadata? metadata = await dbContext.PlayerMetadata
			.FirstOrDefaultAsync(value => value.ProfileId == profileId);
		if (metadata is null)
		{
			PlayerMetadata seed = await GetAsync(profileId);
			metadata = new PlayerMetadata
			{
				ProfileId = seed.ProfileId,
				CountryCode = seed.CountryCode,
				CountryId = seed.CountryId,
				Locale = seed.Locale
			};
			dbContext.PlayerMetadata.Add(metadata);
		}

		if (!string.IsNullOrWhiteSpace(countryCode))
			metadata.CountryCode = NormalizeCountryCode(countryCode);
		if (countryId.HasValue)
			metadata.CountryId = Math.Max(0, countryId.Value);
		if (!string.IsNullOrWhiteSpace(locale))
			metadata.Locale = locale.Trim();
		metadata.LastSeenAt = DateTime.UtcNow;
		await dbContext.SaveChangesAsync();
		return metadata;
	}

	private static string NormalizeCountryCode(string? value)
	{
		string normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
		return normalized.Length == 2 ? normalized : "NL";
	}
}
