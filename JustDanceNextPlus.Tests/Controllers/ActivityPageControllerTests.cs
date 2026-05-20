using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.Services;
using JustDanceNextPlus.Utilities;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ActivityPageController = JustDanceNextPlus.Controllers.prod_next.just_dance.com.editorial.v1.activity_page.formatVersion.V0;

using Moq;

using System.Collections.Immutable;
using System.Text.Json;

namespace JustDanceNextPlus.Tests.Controllers;

public class ActivityPageControllerTests
{
	[Fact]
	public void ActivityPageController_ExposesPcDebugCarouselRoute()
	{
		string[] routeTemplates = typeof(ActivityPageController)
			.GetCustomAttributes(typeof(RouteAttribute), inherit: false)
			.Cast<RouteAttribute>()
			.Select(attribute => attribute.Template)
			.ToArray()!;

		Assert.Contains("editorial/v1/activity-page/formatVersion/v0", routeTemplates);
		Assert.Contains("carousel/v3/activity-page/formatVersion/v0", routeTemplates);
	}

	[Fact]
	public void GetActivityPage_WhenCarouselV3Request_ReturnsLegacyListShape()
	{
		Guid categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
		Guid songId = Guid.Parse("22222222-2222-2222-2222-222222222222");

		ActivityPageResponse activityPage = new()
		{
			Categories = ImmutableDictionary<Guid, ICategory>.Empty.Add(
				categoryId,
				new CarouselCategory
				{
					CategoryName = "RecentlyAdded",
					TitleId = new OasisTag(new LocalizedString(1234, "Recently Added")),
					RecommendedItemType = "map",
					ItemList = ImmutableList.Create(new MapTag { Guid = songId })
				}),
			CategoryModifiers = ImmutableList<IModifier>.Empty.Add(
				new PositionModifier("carousel", categoryId, 0))
		};

		Mock<IActivityPageService> activityPageService = new();
		activityPageService.Setup(service => service.ActivityPage).Returns(activityPage);

		ActivityPageController controller = new(activityPageService.Object)
		{
			ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
			}
		};
		controller.ControllerContext.HttpContext.Request.Path = "/carousel/v3/activity-page/formatVersion/v0";

		OkObjectResult result = Assert.IsType<OkObjectResult>(controller.GetActivityPage());
		JsonElement body = JsonSerializer.SerializeToElement(
			result.Value,
			new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

		JsonElement categories = body.GetProperty("categories");
		Assert.Equal(JsonValueKind.Array, categories.ValueKind);

		JsonElement category = categories.EnumerateArray().Single();
		Assert.Equal(categoryId, category.GetProperty("id").GetGuid());
		Assert.Equal("carousel", category.GetProperty("type").GetString());
		Assert.Equal(1234, category.GetProperty("titleId").GetInt32());

		JsonElement item = category.GetProperty("itemList").EnumerateArray().Single();
		Assert.Equal(songId, item.GetProperty("id").GetGuid());
		Assert.Equal("map", item.GetProperty("type").GetString());

		JsonElement modifier = body.GetProperty("categoryModifiers").EnumerateArray().Single();
		Assert.Equal("position", modifier.GetProperty("name").GetString());
		Assert.Equal("carousel", modifier.GetProperty("itemTypeToModify").GetString());
	}
}
