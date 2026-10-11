using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace JustDanceServer.Services;

public interface IClassicProfileService
{
    ValueTask<IReadOnlyList<JsonObject>> GetProfilesAsync(IEnumerable<Guid> profileIds);
    ValueTask<ClassicProfile?> GetClassicProfileAsync(Guid profileId);
    ValueTask<IReadOnlyList<Guid>> FilterExistingProfileIdsAsync(IEnumerable<Guid> profileIds);
    ValueTask<bool> SaveProfileAsync(Guid profileId, JsonElement payload, string platform);
}

public sealed class ClassicProfileService(
    UserDataContext dbContext,
    IScoreService scoreService,
    IProgressionService progressionService,
    IPlayerMetadataService metadataService,
    ILogger<ClassicProfileService> logger) : IClassicProfileService
{
    public async ValueTask<IReadOnlyList<JsonObject>> GetProfilesAsync(IEnumerable<Guid> profileIds)
    {
        Guid[] ids = [.. profileIds.Where(id => id != Guid.Empty).Distinct()];
        if (ids.Length == 0)
            return [];

        Dictionary<Guid, Profile> profiles = await dbContext.Profiles.AsNoTracking()
            .Include(profile => profile.Dancercard)
            .Where(profile => ids.Contains(profile.Id))
            .ToDictionaryAsync(profile => profile.Id);
        Dictionary<Guid, ClassicProfile> classicProfiles = await dbContext.ClassicProfiles.AsNoTracking()
            .Where(profile => ids.Contains(profile.ProfileId))
            .ToDictionaryAsync(profile => profile.ProfileId);
        ILookup<Guid, MapStats> scores = (await dbContext.MapScores.AsNoTracking()
            .Where(score => ids.Contains(score.ProfileId))
            .ToListAsync())
            .ToLookup(score => score.ProfileId);

        List<JsonObject> responses = [];
        foreach (Guid id in ids)
        {
            if (!profiles.TryGetValue(id, out Profile? profile))
                continue;

            classicProfiles.TryGetValue(id, out ClassicProfile? classicProfile);
            responses.Add(CreateResponse(profile, classicProfile, scores[id]));
        }

        return responses;
    }

    public ValueTask<ClassicProfile?> GetClassicProfileAsync(Guid profileId)
        => new(dbContext.ClassicProfiles.AsNoTracking()
            .FirstOrDefaultAsync(profile => profile.ProfileId == profileId));

    public async ValueTask<IReadOnlyList<Guid>> FilterExistingProfileIdsAsync(
        IEnumerable<Guid> profileIds)
    {
        Guid[] requested = [.. profileIds.Where(id => id != Guid.Empty).Distinct()];
        if (requested.Length == 0)
            return [];

        // The canonical player exists independently of which client first created it.
        // Classic profile payloads are synthesized until the player saves JDU data,
        // so JD+ friends must not disappear from JDU's filter-players response.
        HashSet<Guid> existing = [.. await dbContext.Profiles.AsNoTracking()
            .Where(profile => requested.Contains(profile.Id))
            .Select(profile => profile.Id)
            .ToListAsync()];
        return [.. requested.Where(existing.Contains)];
    }

    public async ValueTask<bool> SaveProfileAsync(Guid profileId, JsonElement payload, string platform)
    {
        if (profileId == Guid.Empty || payload.ValueKind != JsonValueKind.Object)
            return false;

        Profile? profile = await dbContext.Profiles
            .Include(value => value.Dancercard)
            .FirstOrDefaultAsync(value => value.Id == profileId);
        if (profile is null)
            return false;

        JsonObject storedPayload = JsonNode.Parse(payload.GetRawText())?.AsObject() ?? [];
        storedPayload.Remove("scores");
        storedPayload.Remove("profileId");

        ClassicProfile? classicProfile = await dbContext.ClassicProfiles
            .FirstOrDefaultAsync(value => value.ProfileId == profileId);
        if (classicProfile is null)
        {
            classicProfile = new ClassicProfile { ProfileId = profileId };
            dbContext.ClassicProfiles.Add(classicProfile);
        }
        else
        {
            MergePersistentProfileProgress(
                ParseStoredPayload(classicProfile.PayloadJson),
                storedPayload);
        }

        classicProfile.Name = GetString(payload, "name") ?? profile.Dancercard.Name;
        classicProfile.Nickname = GetString(payload, "nickname") ?? classicProfile.Nickname;
        classicProfile.AvatarId = GetUInt32(payload, "avatar", classicProfile.AvatarId);
        classicProfile.CountryId = GetInt32(payload, "country", classicProfile.CountryId);
        classicProfile.SkinId = GetUInt32(payload, "skin", classicProfile.SkinId);
        classicProfile.AliasId = GetUInt32(payload, "alias", classicProfile.AliasId);
        classicProfile.AliasGender = GetInt32(payload, "aliasGender", classicProfile.AliasGender);
        classicProfile.PortraitBorderId = GetUInt32(payload, "portraitBorder", classicProfile.PortraitBorderId);
        classicProfile.JdPoints = Math.Max(
            classicProfile.JdPoints,
            GetInt32(payload, "jdPoints", classicProfile.JdPoints));
        classicProfile.PlatformId = GetString(payload, "platformId") ?? classicProfile.PlatformId;
        classicProfile.Platform = string.IsNullOrWhiteSpace(platform) ? classicProfile.Platform : platform;
        classicProfile.PayloadJson = storedPayload.ToJsonString();
        classicProfile.UpdatedAt = DateTime.UtcNow;
        profile.Dancercard.Name = classicProfile.Name;

        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;

        try
        {
            await dbContext.SaveChangesAsync();
            await progressionService.SetProgressionAsync(
                profileId,
                ProgressionFamily.JDUnlimited,
                classicProfile.JdPoints,
                GetInt32(payload, "level", GetInt32(payload, "rank", 1)),
                0);
            await progressionService.GrantUnlocksAsync(
                profileId,
                ProgressionFamily.JDUnlimited,
                GetUnlockKeys(payload),
                "profile-sync");
            await metadataService.UpdateAsync(profileId, countryId: classicProfile.CountryId);
            if (payload.TryGetProperty("scores", out JsonElement scores)
                && scores.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty map in scores.EnumerateObject())
                {
                    if (map.Value.ValueKind != JsonValueKind.Object)
                        continue;

                    int highScore = GetInt32(map.Value, "highest", 0);
                    int playCount = GetInt32(map.Value, "timesPlayed", 0);
                    bool isCoop = GetBoolean(map.Value, "isCoopHighscore", false);
                    if (highScore == 0 && playCount == 0)
                        continue;

                    ScoreUpdateResult result = await scoreService.MergeMapScoreAsync(
                        new HighscorePerformance(),
                        highScore,
                        playCount,
                        profileId,
                        map.Name,
                        classicProfile.Platform,
                        isCoop);
                    if (!result.Success)
                        throw new InvalidOperationException($"Failed to merge score for '{map.Name}'.");
                }
            }

            if (transaction is not null)
                await transaction.CommitAsync();
            return true;
        }
        catch (Exception exception)
        {
            if (transaction is not null)
                await transaction.RollbackAsync();
            logger.LogError(exception, "Failed to save classic profile {ProfileId}", profileId);
            return false;
        }
    }

    private static JsonObject CreateResponse(
        Profile profile,
        ClassicProfile? classicProfile,
        IEnumerable<MapStats> scores)
    {
        JsonObject response = ParseStoredPayload(classicProfile?.PayloadJson);
        response["name"] = classicProfile?.Name ?? profile.Dancercard.Name;
        response["nickname"] = classicProfile?.Nickname ?? "Player 1";
        response["avatar"] = classicProfile?.AvatarId ?? 0;
        response["country"] = classicProfile?.CountryId ?? 0;
        response["skin"] = classicProfile?.SkinId ?? 0u;
        response["alias"] = classicProfile?.AliasId ?? 0u;
        response["aliasGender"] = classicProfile?.AliasGender ?? 0;
        response["portraitBorder"] = classicProfile?.PortraitBorderId ?? 0u;
        response["jdPoints"] = classicProfile?.JdPoints ?? 0;
        response["platformId"] = classicProfile?.PlatformId ?? profile.Id.ToString();
        response["platform"] = classicProfile?.Platform ?? string.Empty;
        response["isExisting"] = classicProfile is not null;
        response["profileId"] = profile.Id.ToString();

        JsonObject scoreObject = [];
        foreach (MapStats score in scores.OrderBy(score => score.MapName, StringComparer.OrdinalIgnoreCase))
        {
            scoreObject[score.MapName] = new JsonObject
            {
                ["highest"] = score.HighScore,
                ["isCoopHighscore"] = score.IsCoopHighScore,
                ["timesPlayed"] = score.PlayCount
            };
        }

        response["scores"] = scoreObject;
        return response;
    }

    private static JsonObject ParseStoredPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return [];

        try
        {
            return JsonNode.Parse(payload)?.AsObject() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void MergePersistentProfileProgress(JsonObject previous, JsonObject current)
    {
        string[] fields =
        [
            "unlockedAvatars",
            "unlockedSkins",
            "unlockedAliases",
            "unlockedPortraitBorders",
            "unlockedItems",
            "items"
        ];
        foreach (string field in fields)
        {
            if (previous[field] is not JsonArray previousValues)
                continue;

            if (current[field] is not JsonArray currentValues)
            {
                current[field] = previousValues.DeepClone();
                continue;
            }

            HashSet<string> values = [.. currentValues.Select(GetStableJsonValue)];
            foreach (JsonNode? value in previousValues)
            {
                if (values.Add(GetStableJsonValue(value)))
                    currentValues.Add(value?.DeepClone());
            }
        }

        if (previous["stats"] is not JsonObject previousStats)
            return;

        JsonObject currentStats = current["stats"] as JsonObject ?? [];
        current["stats"] = currentStats;
        foreach (string field in new[]
        {
            "stars",
            "danceTime",
            "mapsPlayed",
            "quests",
            "bestPerfectStreak"
        })
        {
            if (!TryGetInt64(previousStats[field], out long previousValue))
                continue;

            long value = TryGetInt64(currentStats[field], out long currentValue)
                ? Math.Max(previousValue, currentValue)
                : previousValue;
            currentStats[field] = value;
        }
    }

    private static bool TryGetInt64(JsonNode? node, out long value)
    {
        if (node is JsonValue jsonValue && jsonValue.TryGetValue(out value))
            return true;

        value = 0;
        return false;
    }

    private static string GetStableJsonValue(JsonNode? value)
        => value?.ToJsonString() ?? "null";

    private static int GetInt32(JsonElement payload, string propertyName, int fallback)
        => payload.TryGetProperty(propertyName, out JsonElement value)
            && value.TryGetInt32(out int parsed)
                ? parsed
                : fallback;

    private static uint GetUInt32(JsonElement payload, string propertyName, uint fallback)
        => payload.TryGetProperty(propertyName, out JsonElement value)
            && value.TryGetUInt32(out uint parsed)
                ? parsed
                : fallback;

    private static string? GetString(JsonElement payload, string propertyName)
        => payload.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;

    private static bool GetBoolean(JsonElement payload, string propertyName, bool fallback)
        => payload.TryGetProperty(propertyName, out JsonElement value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? value.GetBoolean()
                : fallback;

    private static IEnumerable<string> GetUnlockKeys(JsonElement payload)
    {
        string[] fields =
        [
            "unlockedAvatars",
            "unlockedSkins",
            "unlockedAliases",
            "unlockedPortraitBorders",
            "unlockedItems",
            "items"
        ];
        foreach (string field in fields)
        {
            if (!payload.TryGetProperty(field, out JsonElement values)
                || values.ValueKind != JsonValueKind.Array)
                continue;

            foreach (JsonElement value in values.EnumerateArray())
            {
                string? key = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.GetRawText(),
                    _ => null
                };
                if (!string.IsNullOrWhiteSpace(key))
                    yield return $"{field}:{key}";
            }
        }
    }
}
