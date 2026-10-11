using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Catalog;

using Microsoft.Extensions.Options;

using System.Globalization;
using System.Text.Json.Nodes;

namespace JustDanceServer.Services;

public sealed class JduAvatarWireProjection(IOptions<UrlSettings> urlOptions)
{
    private readonly UrlSettings urls = urlOptions.Value;

    public JsonObject Create(IReadOnlyDictionary<uint, JduAvatarDefinition> avatars)
    {
        string cdnBaseUrl = JduContentUrlResolver.GetCdnBaseUrl(urls);
        JsonObject result = [];
        foreach (JduAvatarDefinition avatar in avatars.Values.OrderBy(avatar => avatar.Id))
        {
            JsonObject item = new()
            {
                ["__class"] = "OnlineCustomizableItem",
                ["jdVersion"] = avatar.JdVersion,
                ["mojoPrice"] = avatar.Price,
                ["status"] = (int)avatar.Status,
                ["unlockType"] = (int)avatar.UnlockType,
                ["url"] = CreateCdnUrl(cdnBaseUrl, avatar.ImagePath),
                ["itemType"] = 0,
                ["visibility"] = 1,
                ["relativeSongName"] = avatar.RelatedSong ?? string.Empty,
                ["relativeQuestID"] = avatar.RelatedQuest ?? string.Empty,
                ["relativeWDFBossName"] = avatar.RelatedWdfBoss ?? string.Empty
            };

            if (avatar.MapName is not null)
            {
                item["UsedAsCoach_MapName"] = avatar.MapName;
                item["UsedAsCoach_CoachId"] = avatar.Coach;
            }

            if (avatar.SoundFamily is not null)
                item["soundFamily"] = avatar.SoundFamily;
            if (avatar.SoundPaths.Count != 0)
            {
                item["sounds"] = new JsonArray(
                    avatar.SoundPaths.Select(path => JsonValue.Create(CreateCdnUrl(cdnBaseUrl, path))).ToArray());
            }

            result.Add(avatar.Id.ToString(CultureInfo.InvariantCulture), item);
        }

        return result;
    }

    private static string CreateCdnUrl(string cdnBaseUrl, string relativePath)
        => $"{cdnBaseUrl}/{CdnContentPaths.JduCatalogAssetsRoot}/{relativePath}";
}
