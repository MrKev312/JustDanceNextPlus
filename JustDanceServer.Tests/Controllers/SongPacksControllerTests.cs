using Microsoft.AspNetCore.Mvc;

using SongPacksController = JustDanceServer.Controllers.prod_next.just_dance.com.songdb.v3.Songpacks;

namespace JustDanceServer.Tests.Controllers;

public class SongPacksControllerTests
{
	[Fact]
	public void SongPacksController_ExposesPcDebugSongPacksRoute()
	{
		RouteAttribute route = Assert.Single(typeof(SongPacksController)
			.GetCustomAttributes(typeof(RouteAttribute), inherit: false)
			.Cast<RouteAttribute>());

		Assert.Equal("songdb/v3/songpacks", route.Template);

		HttpGetAttribute httpGet = Assert.Single(typeof(SongPacksController)
			.GetMethod(nameof(SongPacksController.Get))!
			.GetCustomAttributes(typeof(HttpGetAttribute), inherit: false)
			.Cast<HttpGetAttribute>());
		Assert.Null(httpGet.Template);
	}
}
