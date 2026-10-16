using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;

using System.Text.Json;

namespace JustDanceServer.Services;

// Profiles remain mutable because they are EF Core entities.
public sealed class UserProfileService(
    UserDataContext dbContext,
    ILogger<UserProfileService> logger,
    IBundleService? bundleService = null,
    IProgressionService? progressionService = null) : IUserProfileService
{
    public async ValueTask<Profile?> GetProfileByIdAsync(Guid id)
    {
        Profile? profile = await dbContext.Profiles.AsNoTracking()
            .Include(value => value.Dancercard)
            .FirstOrDefaultAsync(value => value.Id == id);
        if (profile is null)
            return null;

        profile.MapStats = await dbContext.MapScores
            .AsNoTracking()
            .Where(score => score.ProfileId == id)
            .ToDictionaryAsync(score => score.MapName, StringComparer.OrdinalIgnoreCase);
        profile.PlaylistStats = await dbContext.PlaylistScores
            .AsNoTracking()
            .Where(score => score.ProfileId == id)
            .ToDictionaryAsync(score => score.PlaylistId);
        ApplyOwnershipClaims(profile);
        await ApplyProgressionAsync(profile);
        return profile;
    }

    public async ValueTask<Profile?> GetProfileByTicketAsync(string ticket)
    {
        Profile? profile = await dbContext.Profiles.AsNoTracking()
            .Include(value => value.Dancercard)
            .FirstOrDefaultAsync(profile => profile.Ticket == ticket);
        if (profile is not null)
        {
            ApplyOwnershipClaims(profile);
            await ApplyProgressionAsync(profile);
        }

        return profile;
    }

    public async ValueTask<IReadOnlyDictionary<Guid, Profile>> GetProfilesByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        Dictionary<Guid, Profile> profiles = [];
        // Bound SQL parameter counts without changing client ordering or duplicate semantics.
        foreach (Guid[] chunk in ids.Distinct().Chunk(500))
        {
            List<Profile> batch = await dbContext.Profiles.AsNoTracking()
                .Include(value => value.Dancercard).Where(value => chunk.Contains(value.Id)).ToListAsync();
            if (batch.Count == 0)
                continue;
            ILookup<Guid, MapStats> mapScores = (await dbContext.MapScores.AsNoTracking()
                .Where(value => chunk.Contains(value.ProfileId)).ToListAsync()).ToLookup(value => value.ProfileId);
            ILookup<Guid, PlaylistStats> playlistScores = (await dbContext.PlaylistScores.AsNoTracking()
                .Where(value => chunk.Contains(value.ProfileId)).ToListAsync()).ToLookup(value => value.ProfileId);
            Dictionary<Guid, PlayerProgression> progressions = progressionService is null ? []
                : await dbContext.PlayerProgressions.AsNoTracking()
                    .Where(value => chunk.Contains(value.ProfileId) && value.Family == ProgressionFamily.JDPlus)
                    .ToDictionaryAsync(value => value.ProfileId);
            foreach (Profile profile in batch)
            {
                profile.MapStats = mapScores[profile.Id].ToDictionary(value => value.MapName, StringComparer.OrdinalIgnoreCase);
                profile.PlaylistStats = playlistScores[profile.Id].ToDictionary(value => value.PlaylistId);
                ApplyOwnershipClaims(profile);
                if (progressions.TryGetValue(profile.Id, out PlayerProgression? progression))
                {
                    profile.CurrentXP = progression.Xp;
                    profile.CurrentLevel = progression.Level;
                    profile.PrestigeGrade = progression.Prestige;
                }

                profiles.Add(profile.Id, profile);
            }
        }

        return profiles;
    }

    public ValueTask<bool> HasJustDanceProfileAsync(Guid id)
        => new(dbContext.Profiles.AnyAsync(profile => profile.Id == id));

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

        ApplyOwnershipClaims(profile);
        dbContext.Profiles.Add(profile);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async ValueTask<bool> UpdateProfileAsync(Profile profile)
    {
        try
        {
            Profile? stored = await dbContext.Profiles
                .Include(value => value.Dancercard)
                .FirstOrDefaultAsync(value => value.Id == profile.Id);
            if (stored is null)
                return false;

            // The caller may supply cosmetics, never a replacement database identity.
            stored.Dancercard.CopyCosmeticsFrom(profile.Dancercard);
            profile.Dancercard.Id = stored.Dancercard.Id;
            SavedDancerCard? primary = await dbContext.SavedDancerCards
                .SingleOrDefaultAsync(card => card.ProfileId == profile.Id && card.Id == stored.Dancercard.Id);
            if (primary is not null)
            {
                primary.CosmeticsJson = JsonSerializer.Serialize(stored.Dancercard);
                primary.UpdatedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to update profile");
            return false;
        }
    }

    private void ApplyOwnershipClaims(Profile profile)
    {
        if (bundleService is not null)
            profile.Ownership.Claims = bundleService.Claims;
    }

    private async ValueTask ApplyProgressionAsync(Profile profile)
    {
        if (progressionService is null)
            return;

        ProgressionSnapshot progression = await progressionService.GetAsync(
            profile.Id,
            ProgressionFamily.JDPlus);
        profile.CurrentXP = progression.Xp;
        profile.CurrentLevel = progression.Level;
        profile.PrestigeGrade = progression.Prestige;
    }
}
