using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Catalog;

using Microsoft.Extensions.Options;

using System.Globalization;
using System.Text.Json.Nodes;

namespace JustDanceServer.Services;

public sealed class JduCatalogWireProjection(IOptions<UrlSettings> urlOptions)
{
    private static readonly uint[] ClientQuestIds = [1];
    private readonly string cdnBaseUrl = JduContentUrlResolver.GetCdnBaseUrl(urlOptions.Value);

    public JsonObject CreateSkins(IReadOnlyDictionary<uint, JduSkinDefinition> skins)
    {
        JsonObject result = [];
        foreach (JduSkinDefinition skin in skins.Values.OrderBy(value => value.Id))
        {
            result.Add(skin.Id.ToString(CultureInfo.InvariantCulture), new JsonObject
            {
                ["__class"] = "OnlineCustomizableItem",
                ["jdVersion"] = skin.JdVersion,
                ["mojoPrice"] = skin.Price,
                ["status"] = (int)skin.Status,
                ["unlockType"] = (int)skin.UnlockType,
                ["url"] = CreateCdnUrl(skin.ImagePath),
                ["itemType"] = 1,
                ["visibility"] = 1
            });
        }

        return result;
    }

    public JsonObject CreatePortraitBorders(IReadOnlyDictionary<uint, JduPortraitBorderDefinition> borders)
    {
        JsonObject result = [];
        foreach (JduPortraitBorderDefinition border in borders.Values.OrderBy(value => value.Id))
        {
            JsonObject item = new()
            {
                ["__class"] = "OnlinePortraitBorder",
                ["backgroundUrl"] = CreateCdnUrl(border.BackgroundPath),
                ["originalLockStatus"] = border.OriginalLockStatus,
                ["visibility"] = border.Visibility
            };
            if (border.ForegroundPath is not null)
                item["foregroundUrl"] = CreateCdnUrl(border.ForegroundPath);
            result.Add(border.Id.ToString(CultureInfo.InvariantCulture), item);
        }

        return result;
    }

    public JsonObject CreateAliases(IReadOnlyDictionary<uint, JduAliasDefinition> aliases)
    {
        JsonObject result = [];
        foreach (JduAliasDefinition alias in aliases.Values.OrderBy(value => value.Id))
        {
            JsonObject item = new()
            {
                ["DifficultyColor"] = alias.DifficultyColor,
                ["StringLocID"] = alias.StringLocId,
                ["StringPlaceholder"] = alias.Placeholder,
                ["__class"] = "JD_UnlockableAliasDescriptor",
                ["StringOnlineLocalized"] = alias.Localized,
                ["StringOnlineLocalizedFemale"] = alias.LocalizedFemale,
                ["DescriptionLocalized"] = alias.Description,
                ["Visibility"] = alias.Visibility
            };
            if (alias.DescriptionLocId is uint descriptionLocId)
                item["DescriptionLocID"] = descriptionLocId;
            if (alias.RestrictedToUnlimitedSongs is bool restricted)
                item["RestrictedToUnlimitedSongs"] = restricted ? 1 : 0;
            if (alias.StringLocIdFemale is uint femaleLocId)
                item["StringLocIDFemale"] = femaleLocId;
            if (alias.Unlock is { } unlock)
            {
                JsonObject objective = new()
                {
                    ["Description"] = unlock.Description,
                    ["MinimumValue"] = unlock.MinimumValue,
                    ["__class"] = unlock.Class
                };
                if (unlock.ObjectiveType is int objectiveType)
                    objective["ObjectiveType"] = objectiveType;
                if (unlock.MinimumStars is int minimumStars)
                    objective["MinStarToReach"] = minimumStars;
                item["UnlockObjective"] = objective;
            }

            result.Add(alias.Id.ToString(CultureInfo.InvariantCulture), item);
        }

        return result;
    }

    public JsonArray CreateQuests(
        IReadOnlyDictionary<uint, JduQuestDefinition> quests,
        string? assetPlatform)
    {
        string platform = JduAssetPlatform.Normalize(assetPlatform)
            ?? throw new InvalidDataException("JDU quests require a recognized asset platform.");
        return [with(quests.Values
            .OrderBy(value => value.Id)
            .Select(quest =>
            {
                if (!quest.PlatformAssets.TryGetValue(platform, out JduQuestPlatformAssets? platformAssets))
                    throw new InvalidDataException($"JDU quest '{quest.Id}' has no assets for '{platform}'.");
                return (JsonNode)new JsonObject
                {
                    ["__class"] = "OnlineQuest",
                    ["assetUrls"] = new JsonObject
                    {
                        ["phoneImageURL"] = CreateCdnUrl(quest.PhoneImagePath),
                        ["coverImageURL"] = CreateCdnUrl(platformAssets.CoverPath),
                        ["logoImageURL"] = CreateCdnUrl(platformAssets.LogoPath),
                        ["logoShadedImageURL"] = CreateCdnUrl(platformAssets.ShadedLogoPath)
                    },
                    ["id"] = quest.Id.ToString(CultureInfo.InvariantCulture),
                    ["locked"] = quest.Locked ? 1 : 0,
                    ["playlist"] = new JsonArray([.. quest.MapNames.Select(mapName => (JsonNode?)JsonValue.Create(mapName))]),
                    ["title"] = quest.Title
                };
            })
            .ToArray())];
    }

    public void PopulateQuestCarousel(
        JsonObject carousel,
        IReadOnlyDictionary<uint, JduQuestDefinition> quests)
    {
        if (carousel["categories"] is not JsonArray categories)
            throw new InvalidDataException("JDU quest carousel has no categories array.");
        JsonObject? unlimitedRow = categories
            .OfType<JsonObject>()
            .FirstOrDefault(category => category["isc"]?.GetValue<string>() == "quest_row_unlimited") ?? throw new InvalidDataException("JDU quest carousel has no unlimited quest row.");
        IEnumerable<uint> questIds = quests.Keys
            .Concat(ClientQuestIds)
            .Distinct()
            .OrderDescending();
        unlimitedRow["items"] = new JsonArray(questIds.Select(id => (JsonNode)new JsonObject
        {
            ["__class"] = "Item",
            ["isc"] = "quest_item",
            ["act"] = "ui_component_base",
            ["components"] = new JsonArray(new JsonObject
            {
                ["__class"] = "JD_CarouselContentComponent_Quest",
                ["questId"] = id.ToString(CultureInfo.InvariantCulture)
            }),
            ["actionList"] = "questUnlocked"
        }).ToArray());
    }

    private string CreateCdnUrl(string relativePath)
        => $"{cdnBaseUrl}/{CdnContentPaths.JduCatalogAssetsRoot}/{relativePath}";
}
