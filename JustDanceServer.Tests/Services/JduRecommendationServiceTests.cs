using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.EntityFrameworkCore;

using Moq;

namespace JustDanceServer.Tests.Services;

public sealed class JduRecommendationServiceTests
{
	[Fact]
	public async Task RecommendationsUseSharedPlayCountsAndCatalogMetadata()
	{
		DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		await using UserDataContext context = new(options);
		context.MapScores.AddRange(
			new MapStats { MapName = "Popular", ProfileId = Guid.NewGuid(), PlayCount = 8 },
			new MapStats { MapName = "Popular", ProfileId = Guid.NewGuid(), PlayCount = 4 },
			new MapStats { MapName = "OtherArtist", ProfileId = Guid.NewGuid(), PlayCount = 1 });
		await context.SaveChangesAsync();

		OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
		AddSong(songs, "Popular", "Artist A", 2022);
		AddSong(songs, "SameArtist", "Artist A", 2018);
		AddSong(songs, "SameYear", "Artist B", 2022);
		AddSong(songs, "OtherArtist", "Artist C", 2017);
		Mock<IMapService> mapService = new();
		mapService.SetupGet(service => service.Maps).Returns(songs.ToServerMaps());
		JduRecommendationService service = new(new RecommendationService(context, mapService.Object, TimeProvider.System));

		string[] ordered = await service.GetOrderedMapsAsync();
		IReadOnlyDictionary<string, string[]> related = await service.GetRelatedSongsAsync();

		Assert.Equal("Popular", ordered[0]);
		Assert.Equal("OtherArtist", ordered[1]);
		Assert.Equal("SameArtist", related["Popular"][0]);
		Assert.Equal("SameYear", related["Popular"][1]);
	}

	private static void AddSong(
		OrderedDictionary<Guid, JustDanceSongDBEntry> songs,
		string mapName,
		string artist,
		int version)
	{
		songs[Guid.NewGuid()] = new JustDanceSongDBEntry
		{
			MapName = mapName,
			Title = mapName,
			Artist = artist,
			OriginalJDVersion = version,
			DanceVersionLocId = new LocalizedString(0, string.Empty)
		};
	}
}
