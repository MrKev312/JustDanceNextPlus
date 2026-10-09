using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;

namespace JustDanceServer.Services;

public sealed record ResolvedPlayerIdentity(
	Guid ProfileId,
	string PlatformType,
	string IdOnPlatform,
	string NameOnPlatform,
	string SessionKey);

public sealed record LinkedProfileIdentity(
	Guid ProfileId,
	Guid UserId,
	string PlatformType,
	string IdOnPlatform,
	string NameOnPlatform);

public interface IPlayerIdentityService
{
	ValueTask<ResolvedPlayerIdentity> ResolveOrCreateAsync(SessionLoginIdentity login);
	ValueTask<IReadOnlyList<LinkedProfileIdentity>> GetLinkedProfilesAsync(Guid userId);
	ValueTask<IReadOnlyList<LinkedProfileIdentity>> GetProfilesByIdsAsync(
		IEnumerable<Guid> profileIds,
		string? platformType = null);
	ValueTask<IReadOnlyList<LinkedProfileIdentity>> GetProfilesByPlatformIdsAsync(
		IEnumerable<string> platformIds,
		string? platformType);
	ValueTask<IReadOnlyList<LinkedProfileIdentity>> GetProfilesByNamesAsync(
		IEnumerable<string> names,
		string? platformType);
}

public sealed class PlayerIdentityService(
	UserDataContext dbContext,
	IPlayerMetadataService metadataService) : IPlayerIdentityService
{
	public async ValueTask<ResolvedPlayerIdentity> ResolveOrCreateAsync(SessionLoginIdentity login)
	{
		PlatformIdentity? platformIdentity = await dbContext.PlatformIdentities
			.FirstOrDefaultAsync(identity =>
				identity.Provider == login.Provider && identity.Subject == login.Subject);

		Profile? profile;
		if (platformIdentity is not null)
		{
			profile = await LoadProfileAsync(platformIdentity.ProfileId)
				?? throw new InvalidOperationException("A platform identity references a missing profile.");
			UpdateIdentity(platformIdentity, profile, login);
			await dbContext.SaveChangesAsync();
		}
		else
		{
			// Adopt old rows on their first login. Previous versions stored the parsed
			// credential directly in Profile.Ticket, so this preserves scores and JDU data.
			// Only verified Nintendo identities may adopt the old unqualified subject/ticket rows.
			// A Basic username is never evidence of ownership of a legacy platform account.
			profile = login.Provider == "nintendo-account" ? await dbContext.Profiles
				.Include(value => value.Dancercard)
				.FirstOrDefaultAsync(value =>
					(value.Ticket == login.LegacyTicket || value.Ticket == login.Subject)
					&& !dbContext.PlatformIdentities.Any(identity => identity.ProfileId == value.Id
						&& identity.Provider != login.Provider)) : null;

			if (profile is null)
			{
				Guid profileId = Guid.NewGuid();
				profile = new Profile
				{
					Id = profileId,
					Dancercard = new DancerCard
					{
						Id = profileId,
						Name = login.NameOnPlatform
					}
				};
				dbContext.Profiles.Add(profile);
			}

			platformIdentity = new PlatformIdentity
			{
				Provider = login.Provider,
				Subject = login.Subject,
				ProfileId = profile.Id
			};
			dbContext.PlatformIdentities.Add(platformIdentity);
			UpdateIdentity(platformIdentity, profile, login);
			try
			{
				await dbContext.SaveChangesAsync();
			}
			catch (DbUpdateException)
			{
				// Two games can establish sessions for the same console account at
				// the same time. The composite identity key chooses the winner; the
				// losing request simply adopts that canonical player.
				dbContext.ChangeTracker.Clear();
				platformIdentity = await dbContext.PlatformIdentities
					.FirstOrDefaultAsync(identity =>
						identity.Provider == login.Provider
						&& identity.Subject == login.Subject);
				if (platformIdentity is null)
					throw;

				profile = await LoadProfileAsync(platformIdentity.ProfileId)
					?? throw new InvalidOperationException(
						"A concurrently-created platform identity references a missing profile.");
				UpdateIdentity(platformIdentity, profile, login);
				await dbContext.SaveChangesAsync();
			}
		}

		await metadataService.UpdateAsync(profile.Id);

		return new ResolvedPlayerIdentity(
			profile.Id,
			platformIdentity.PlatformType,
			platformIdentity.IdOnPlatform,
			platformIdentity.NameOnPlatform,
			login.SessionKey);
	}

	public async ValueTask<IReadOnlyList<LinkedProfileIdentity>> GetLinkedProfilesAsync(Guid userId)
	{
		Profile? profile = await LoadProfileAsync(userId);
		if (profile is null)
			return [];

		List<PlatformIdentity> identities = await dbContext.PlatformIdentities
			.AsNoTracking()
			.Where(identity => identity.ProfileId == userId)
			.OrderBy(identity => identity.PlatformType)
			.ToListAsync();

		List<LinkedProfileIdentity> result =
		[
			.. identities.Select(identity => ToLinkedProfile(profile, identity))
		];
		AddSyntheticUplayProfile(result, profile);
		return result;
	}

	public async ValueTask<IReadOnlyList<LinkedProfileIdentity>> GetProfilesByIdsAsync(
		IEnumerable<Guid> profileIds,
		string? platformType = null)
	{
		Guid[] ids = [.. profileIds.Where(id => id != Guid.Empty).Distinct()];
		if (ids.Length == 0)
			return [];

		List<Profile> profiles = await dbContext.Profiles.AsNoTracking()
			.Include(profile => profile.Dancercard)
			.Where(profile => ids.Contains(profile.Id))
			.ToListAsync();
		return await BuildRequestedProfilesAsync(profiles, platformType);
	}

	public async ValueTask<IReadOnlyList<LinkedProfileIdentity>> GetProfilesByPlatformIdsAsync(
		IEnumerable<string> platformIds,
		string? platformType)
	{
		string[] ids = [.. platformIds
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim())
			.Distinct(StringComparer.Ordinal)];
		if (ids.Length == 0)
			return [];

		string? normalizedPlatform = NormalizePlatform(platformType);
		IQueryable<PlatformIdentity> query = dbContext.PlatformIdentities.AsNoTracking()
			.Where(identity => ids.Contains(identity.IdOnPlatform));
		if (normalizedPlatform is not null)
			query = query.Where(identity => identity.PlatformType == normalizedPlatform);

		List<PlatformIdentity> identities = await query.ToListAsync();
		List<LinkedProfileIdentity> result = await BuildIdentityProfilesAsync(identities);

		// Uplay profile IDs are the canonical account GUID in the legacy friends API.
		if (normalizedPlatform is null or "uplay")
		{
			Guid[] canonicalIds = [.. ids.Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty)
				.Where(id => id != Guid.Empty)];
			foreach (LinkedProfileIdentity profile in await GetProfilesByIdsAsync(canonicalIds, "uplay"))
			{
				if (result.All(existing => existing.ProfileId != profile.ProfileId
					|| existing.PlatformType != profile.PlatformType))
					result.Add(profile);
			}
		}

		return result;
	}

	public async ValueTask<IReadOnlyList<LinkedProfileIdentity>> GetProfilesByNamesAsync(
		IEnumerable<string> names,
		string? platformType)
	{
		string[] requestedNames = [.. names
			.Where(value => !string.IsNullOrWhiteSpace(value))
			.Select(value => value.Trim().ToLowerInvariant())
			.Distinct(StringComparer.Ordinal)];
		if (requestedNames.Length == 0)
			return [];

		List<Profile> profiles = await dbContext.Profiles.AsNoTracking()
			.Include(profile => profile.Dancercard)
			.Where(profile => requestedNames.Contains(profile.Dancercard.Name.ToLower()))
			.ToListAsync();
		return await BuildRequestedProfilesAsync(profiles, platformType);
	}

	private async ValueTask<List<LinkedProfileIdentity>> BuildRequestedProfilesAsync(
		IReadOnlyList<Profile> profiles,
		string? platformType)
	{
		if (profiles.Count == 0)
			return [];

		Guid[] profileIds = [.. profiles.Select(profile => profile.Id)];
		string? normalizedPlatform = NormalizePlatform(platformType);
		IQueryable<PlatformIdentity> query = dbContext.PlatformIdentities.AsNoTracking()
			.Where(identity => profileIds.Contains(identity.ProfileId));
		if (normalizedPlatform is not null and not "uplay")
			query = query.Where(identity => identity.PlatformType == normalizedPlatform);

		ILookup<Guid, PlatformIdentity> identities = (await query.ToListAsync())
			.ToLookup(identity => identity.ProfileId);
		List<LinkedProfileIdentity> result = [];

		foreach (Profile profile in profiles)
		{
			if (normalizedPlatform is not null and not "uplay")
				result.AddRange(identities[profile.Id].Select(identity => ToLinkedProfile(profile, identity)));

			if (normalizedPlatform is null or "uplay")
				AddSyntheticUplayProfile(result, profile);
		}

		return result;
	}

	private async ValueTask<List<LinkedProfileIdentity>> BuildIdentityProfilesAsync(
		IReadOnlyList<PlatformIdentity> identities)
	{
		if (identities.Count == 0)
			return [];

		Guid[] profileIds = [.. identities.Select(identity => identity.ProfileId).Distinct()];
		Dictionary<Guid, Profile> profiles = await dbContext.Profiles.AsNoTracking()
			.Include(profile => profile.Dancercard)
			.Where(profile => profileIds.Contains(profile.Id))
			.ToDictionaryAsync(profile => profile.Id);

		return [.. identities
			.Where(identity => profiles.ContainsKey(identity.ProfileId))
			.Select(identity => ToLinkedProfile(profiles[identity.ProfileId], identity))];
	}

	private ValueTask<Profile?> LoadProfileAsync(Guid profileId)
		=> new(dbContext.Profiles
			.Include(value => value.Dancercard)
			.FirstOrDefaultAsync(value => value.Id == profileId));

	private static LinkedProfileIdentity ToLinkedProfile(
		Profile profile,
		PlatformIdentity identity)
		=> new(
			profile.Id,
			profile.Id,
			identity.PlatformType,
			identity.IdOnPlatform,
			identity.NameOnPlatform);

	private static void AddSyntheticUplayProfile(
		ICollection<LinkedProfileIdentity> profiles,
		Profile profile)
	{
		if (profiles.Any(value => value.PlatformType == "uplay"))
			return;

		profiles.Add(new LinkedProfileIdentity(
			profile.Id,
			profile.Id,
			"uplay",
			profile.Id.ToString(),
			profile.Dancercard.Name));
	}

	private static void UpdateIdentity(
		PlatformIdentity identity,
		Profile profile,
		SessionLoginIdentity login)
	{
		identity.PlatformType = login.PlatformType;
		identity.IdOnPlatform = login.IdOnPlatform;
		identity.NameOnPlatform = login.NameOnPlatform;
		identity.UpdatedAt = DateTime.UtcNow;
		profile.Dancercard.Name = login.NameOnPlatform;
	}

	private static string? NormalizePlatform(string? platformType)
		=> string.IsNullOrWhiteSpace(platformType)
			? null
			: platformType.Trim().ToLowerInvariant();
}
