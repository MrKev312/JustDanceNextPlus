using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

namespace JustDanceServer.Tests.Services;

internal sealed class DiscoveryTestContext : IAsyncDisposable
{
	private readonly SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
	public UserDataContext Db { get; }
	public DiscoveryClock Clock { get; } = new();
	public Mock<IMapService> Maps { get; } = new();
	public Guid Player { get; } = Guid.NewGuid();
	public Guid OtherPlayer { get; } = Guid.NewGuid();
	public ScoreService Scores { get; }
	public PlayerActivityService Activity { get; }
	public PlayerPreferenceService Preferences { get; }
	public RecommendationService Recommendations { get; }

	public DiscoveryTestContext()
	{
		Db = new(new DbContextOptionsBuilder<UserDataContext>().UseSqlite(connection).Options);
		OrderedDictionary<Guid, JustDanceSongDBEntry> songs = [];
		foreach (string name in new[] { "Alpha", "Beta", "Gamma", "Delta", "Echo", "Foxtrot" })
			songs.Add(StableContentId.Create(name), new JustDanceSongDBEntry
			{
				MapName = name, Title = name, Artist = name, OriginalJDVersion = 2022,
				DanceVersionLocId = new LocalizedString(0, string.Empty)
			});
		OrderedDictionary<Guid, ServerMap> catalog = songs.ToServerMaps();
		Maps.SetupGet(value => value.Maps).Returns(catalog);
		Maps.SetupGet(value => value.RecentlyAdded).Returns([]);
		Maps.Setup(value => value.GetMap(It.IsAny<string>())).Returns((string name) =>
			catalog.Values.FirstOrDefault(map => map.Song.MapName == name));
		Maps.Setup(value => value.GetSongId(It.IsAny<string>())).Returns((string name) =>
			catalog.Values.Any(map => map.Song.MapName == name) ? StableContentId.Create(name) : null);
		Scores = new(Db, NullLogger<ScoreService>.Instance);
		Activity = new(Db, Maps.Object, Scores, Clock);
		Preferences = new(Db, Maps.Object, Clock);
		Recommendations = new(Db, Maps.Object, Clock);
	}

	public async Task InitializeAsync()
	{
		await connection.OpenAsync();
		await Db.Database.EnsureCreatedAsync();
		Db.Profiles.AddRange(new Profile { Id = Player, Dancercard = new() { Id = Player, Name = "Player" } },
			new Profile { Id = OtherPlayer, Dancercard = new() { Id = OtherPlayer, Name = "Other" } });
		await Db.SaveChangesAsync();
	}

	public PlayerRequestContext Request(int year = 2022)
		=> new(Player, ProgressionFamily.JDUnlimited, year, "switch", "en-US");

	public async ValueTask DisposeAsync()
	{
		await Db.DisposeAsync();
		await connection.DisposeAsync();
	}
}

internal sealed class DiscoveryClock : TimeProvider
{
	public DateTimeOffset Now { get; set; } = new(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
	public override DateTimeOffset GetUtcNow() => Now;
}
