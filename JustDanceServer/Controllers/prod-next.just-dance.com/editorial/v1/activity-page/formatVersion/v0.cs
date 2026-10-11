using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace JustDanceServer.Controllers.prod_next.just_dance.com.editorial.v1.activity_page.formatVersion;

[ApiController]
[Route("editorial/v1/activity-page/formatVersion/v0")]
[Route("carousel/v3/activity-page/formatVersion/v0")]
public class V0(IActivityPageService activityPageService, IMenuService menuService,
    ISessionManager sessions, IMapService maps) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActivityPage()
    {
        ActivityPageResponse? activityPage = activityPageService.ActivityPage;
        if (activityPage == null)
            return NotFound();
        IReadOnlyList<MenuRow> rows = await menuService.GetRowsAsync("jdnext-activity",
            PlayerRequestContextResolver.FromRequest(Request, sessions));
        activityPage = MenuProjection.AddRows(activityPage, rows, maps);

        return IsCarouselV3Request()
            ? Ok(ToCarouselV3(activityPage))
            : Ok(activityPage);
    }

    private bool IsCarouselV3Request()
    {
        return Request.Path.Value?.StartsWith("/carousel/v3/", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static CarouselV3Response ToCarouselV3(ActivityPageResponse activityPage)
    {
        return new CarouselV3Response(
            [.. activityPage.Categories.Select(ToCarouselV3Category)],
            [.. activityPage.CategoryModifiers.Select(ToCarouselV3Modifier)]);
    }

    private static object ToCarouselV3Category(KeyValuePair<Guid, ICategory> category)
    {
        return category.Value switch
        {
            CarouselCategory carousel => new CarouselV3CarouselCategory(
                category.Key,
                carousel.Type,
                carousel.CategoryName,
                carousel.TitleId.ID,
                carousel.Filters?.Select(ToCarouselV3Filter).ToList() ?? [],
                carousel.ItemList?.Select(item => new CarouselV3Item(
                    item.Guid,
                    ToActivityPageItemType(carousel.RecommendedItemType))).ToList() ?? []),

            BannerCategory banner => new CarouselV3BannerCategory(
                category.Key,
                banner.Type,
                banner.CategoryName,
                banner.TitleId.ID,
                banner.DescriptionId.ID,
                banner.BannerType,
                new CarouselV3Assets(banner.Assets.BackgroundImage, banner.Assets.ForegroundImage),
                banner.Item == null
                    ? null
                    : new CarouselV3Item(banner.Item.Id.Guid, ToActivityPageItemType(banner.Item.Type))),

            _ => new CarouselV3BasicCategory(
                category.Key,
                category.Value.Type,
                category.Value.CategoryName,
                category.Value.TitleId.ID)
        };
    }

    private static Dictionary<string, object?> ToCarouselV3Filter(JustDanceClasses.Database.Filter filter)
    {
        Dictionary<string, object?> carouselFilter = new()
        {
            ["name"] = filter.Name,
            ["itemTypeToFilter"] = ToActivityPageItemType(filter.ItemTypeToFilter)
        };

        if (filter.IncludedTags != null)
            carouselFilter["includedTags"] = filter.IncludedTags.Select(tag => tag.TagGuid).ToList();

        if (filter.ExcludedTags != null)
            carouselFilter["excludedTags"] = filter.ExcludedTags.Select(tag => tag.TagGuid).ToList();

        if (filter.ComparedValue != null)
        {
            string comparedValueName = filter.Name switch
            {
                "matchValue" => "matchValue",
                "maximumValue" => "maximumValue",
                "minimumValue" => "minimumValue",
                _ => "comparedValue"
            };

            carouselFilter[comparedValueName] = new CarouselV3ComparedValue(
                filter.ComparedValue.Name,
                filter.ComparedValue.Value);
        }

        return carouselFilter;
    }

    private static object ToCarouselV3Modifier(IModifier modifier)
    {
        return modifier switch
        {
            PositionModifier position => new CarouselV3PositionModifier(
                "position",
                ToActivityPageItemType(position.ItemTypeToModify),
                new CarouselV3PositionValue(position.Position.Id, position.Position.Position)),

            MaxAmountElements maxAmount => new CarouselV3MaxAmountElement(
                maxAmount.Name,
                ToActivityPageItemType(maxAmount.ItemTypeToFilter),
                new CarouselV3MaxAmountElementValue(
                    ToActivityPageItemType(maxAmount.MaxAmountElement.Type),
                    maxAmount.MaxAmountElement.Value)),

            MoreThanXElements moreThan => new CarouselV3MoreThanXElement(
                moreThan.Name,
                ToActivityPageItemType(moreThan.ItemTypeToModify),
                moreThan.Value),

            _ => new { modifier.Name }
        };
    }

    private static string ToActivityPageItemType(string itemType)
    {
        return itemType.ToLowerInvariant() switch
        {
            "playlist" => "playlist",
            "artist" => "artist",
            "genre" => "genre",
            "banner" => "banner",
            "carousel" => "carousel",
            _ => "map"
        };
    }

    private sealed record CarouselV3Response(
        IReadOnlyList<object> Categories,
        IReadOnlyList<object> CategoryModifiers);

    private sealed record CarouselV3CarouselCategory(
        Guid Id,
        string Type,
        string CategoryName,
        int TitleId,
        IReadOnlyList<Dictionary<string, object?>> Filters,
        IReadOnlyList<CarouselV3Item> ItemList);

    private sealed record CarouselV3BannerCategory(
        Guid Id,
        string Type,
        string CategoryName,
        int TitleId,
        int DescriptionId,
        string BannerType,
        CarouselV3Assets Assets,
        CarouselV3Item? Item);

    private sealed record CarouselV3BasicCategory(
        Guid Id,
        string Type,
        string CategoryName,
        int TitleId);

    private sealed record CarouselV3Item(Guid Id, string Type);

    private sealed record CarouselV3Assets(string BackgroundImage, string? ForegroundImage);

    private sealed record CarouselV3ComparedValue(string Name, int Value);

    private sealed record CarouselV3PositionModifier(
        string Name,
        string ItemTypeToModify,
        CarouselV3PositionValue Position);

    private sealed record CarouselV3PositionValue(Guid Id, int Position);

    private sealed record CarouselV3MaxAmountElement(
        string Name,
        string ItemTypeToFilter,
        CarouselV3MaxAmountElementValue MaxAmountElement);

    private sealed record CarouselV3MaxAmountElementValue(string Type, int Value);

    private sealed record CarouselV3MoreThanXElement(
        string Name,
        string ItemTypeToModify,
        int Value);
}
