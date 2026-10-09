using JustDanceServer.Configuration;
using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.JustDanceClasses.Database.Profile;
using JustDanceServer.JustDanceClasses.Endpoints;
using JustDanceServer.Services;
using JustDanceServer.Utilities;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace JustDanceServer.Tests.Services;

public sealed class WdfServiceTests
{
	private static readonly string[] SharedCatalogMapNames = ["AboutThatBass", "Despacito", "Daddy", "OnMyMind", "PokerFaceALT"];
	private static WdfRoomId SharedRoom => WdfRoomId.HappyHour;

	[Fact]
	public async Task HostedRotationStartsBeforeTheFirstScreenRequest()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB", "MapC");
		WdfDirector rotation = CreateRotation(maps, clock);
		await rotation.StartAsync(CancellationToken.None);
		try
		{
			clock.UtcNow = clock.UtcNow.AddSeconds(40);
			WdfScreenList schedule = rotation.GetScreens(SharedRoom, null);

			Assert.Contains(schedule.Screens, screen => screen.Type == "vote-lobby");
			Assert.Contains(schedule.Screens, screen => screen.Type == "in-game");
		}
		finally
		{
			await rotation.StopAsync(CancellationToken.None);
			rotation.Dispose();
		}
	}

	[Fact]
	public void LateCursorDoesNotReplayExpiredScreens()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB", "MapC");
		WdfDirector rotation = CreateRotation(maps, clock);
		WdfScreen initialVote = Assert.Single(rotation.GetScreens(SharedRoom, null).Screens);
		WdfVoteScreenInfo voteInfo = Assert.IsType<WdfVoteScreenInfo>(initialVote.VoteInfo);

		clock.UtcNow = FromServerTime(voteInfo.VoteResultFetchTime + 0.001);
		rotation.GetVoteResult(SharedRoom);
		WdfScreen recap = rotation.GetScreens(SharedRoom, null).Screens.Single(screen => screen.Type == "vote-recap");
		clock.UtcNow = FromServerTime(recap.EndTime + 5);
		double now = clock.UtcNow.ToUnixTimeMilliseconds() / 1000d;

		WdfScreen resumed = Assert.Single(rotation.GetScreens(SharedRoom, initialVote.EndTime).Screens);

		Assert.Equal("vote", resumed.Type);
		Assert.True(resumed.EndTime > now);
		Assert.Null(resumed.MapName);
	}

	[Fact]
	public void HappyHourScheduleAdvertisesCurrentOrNextCapturedThirtyMinuteWindow()
	{
		TestTimeProvider clock = CreateClock(new DateTimeOffset(2026, 8, 10, 12, 10, 0, TimeSpan.Zero));
		WdfHappyHourSchedule schedule = CreateSchedule(clock);

		WdfHappyHoursInfo running = schedule.GetInfo();
		Assert.True(running.Running);
		Assert.Equal(30 * 60, running.End - running.Start);

		clock.UtcNow = new DateTimeOffset(2026, 8, 10, 12, 31, 0, TimeSpan.Zero);
		WdfHappyHoursInfo next = schedule.GetInfo();
		Assert.False(next.Running);
		Assert.Equal(new DateTimeOffset(2026, 8, 10, 15, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(), next.Start);
	}

	[Fact]
	public void EditionsSplitNormallyAndMergeIntoOneHappyHourRoom()
	{
		TestTimeProvider clock = CreateClock(new DateTimeOffset(2026, 8, 10, 13, 0, 0, TimeSpan.Zero));
		(IMapService maps, _) = CreateMaps("MapA", "MapB");
		using ServiceProvider provider = CreateScopeProvider();
		WdfService service = CreateService(maps, provider, clock);

		Assert.Equal(WdfRoomId.ForEdition(2016), service.GetAssignedRoom(2016));
		Assert.Equal(WdfRoomId.ForEdition(2022), service.GetAssignedRoom(2022));

		clock.UtcNow = new DateTimeOffset(2026, 8, 10, 15, 10, 0, TimeSpan.Zero);
		Assert.Equal(SharedRoom, service.GetAssignedRoom(2016));
		Assert.Equal(SharedRoom, service.GetAssignedRoom(2022));
	}

	[Fact]
	public void EditionRoomsUseBaseSongsAndHappyHourUsesSharedSubscriptionCatalog()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps(SharedCatalogMapNames);
		WdfDirector rotation = CreateRotation(maps, clock);

		WdfVoteScreenInfo jd2018 = GetVote(rotation, WdfRoomId.ForEdition(2018));
		WdfVoteScreenInfo jd2017 = GetVote(rotation, WdfRoomId.ForEdition(2017));
		WdfVoteScreenInfo happyHour = GetVote(rotation, SharedRoom);

		Assert.Equal(["Despacito"], jd2018.VoteOptions);
		Assert.Equal(["Daddy"], jd2017.VoteOptions);
		Assert.Equal(2, happyHour.VoteOptions.Count);
		Assert.All(happyHour.VoteOptions, option => Assert.Contains(option, SharedCatalogMapNames));
		Assert.Equal(["AboutThatBass"], GetVote(rotation, WdfRoomId.ForEdition(2016)).VoteOptions);
	}

	[Fact]
	public void HappyHourMembershipIsSharedAcrossGameYears()
	{
		TestTimeProvider clock = CreateClock(new DateTimeOffset(2026, 8, 10, 12, 10, 0, TimeSpan.Zero));
		(IMapService maps, _) = CreateMaps("MapA", "MapB");
		using ServiceProvider provider = CreateScopeProvider();
		WdfService service = CreateService(maps, provider, clock);
		WdfRoomId jd2016Room = service.GetAssignedRoom(2016);
		WdfRoomId jd2022Room = service.GetAssignedRoom(2022);

		service.Touch(jd2016Room, Guid.NewGuid());
		service.Touch(jd2022Room, Guid.NewGuid());

		Assert.Equal(SharedRoom, jd2016Room);
		Assert.Equal(SharedRoom, jd2022Room);
		Assert.Equal(2, service.GetActivePlayerCount(SharedRoom));
	}

	[Fact]
	public async Task SessionSummaryTracksPlayersMetAcrossTheWholeVisit()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB");
		Guid first = Guid.NewGuid();
		Guid second = Guid.NewGuid();
		Mock<IClassicProfileService> classicProfiles = new();
		classicProfiles.Setup(service => service.GetClassicProfileAsync(first))
			.ReturnsAsync(new ClassicProfile { ProfileId = first, CountryId = 33 });
		classicProfiles.Setup(service => service.GetClassicProfileAsync(second))
			.ReturnsAsync(new ClassicProfile { ProfileId = second, CountryId = 49 });
		using ServiceProvider provider = CreateScopeProvider(classicProfiles: classicProfiles.Object);
		WdfService service = CreateService(maps, provider, clock);

		service.BeginSession(SharedRoom, first);
		service.BeginSession(SharedRoom, second);
		await service.InitializeScoreAsync(SharedRoom, first, "switch");
		await service.InitializeScoreAsync(SharedRoom, second, "switch");

		WdfSessionSummary firstSummary = service.GetSessionSummary(first);
		WdfSessionSummary secondSummary = service.GetSessionSummary(second);
		Assert.Equal(1, firstSummary.UniquePlayerCount);
		Assert.Equal(["49"], firstSummary.Countries);
		Assert.Equal(1, secondSummary.UniquePlayerCount);
		Assert.Equal(["33"], secondSummary.Countries);
	}

	[Fact]
	public void BeginningANewVisitClearsThePreviousEncounters()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB");
		using ServiceProvider provider = CreateScopeProvider();
		WdfService service = CreateService(maps, provider, clock);
		Guid first = Guid.NewGuid();
		Guid second = Guid.NewGuid();

		service.BeginSession(SharedRoom, first);
		service.BeginSession(SharedRoom, second);
		Assert.Equal(1, service.GetSessionSummary(first).UniquePlayerCount);

		service.Leave(SharedRoom, first);
		service.Leave(SharedRoom, second);
		service.BeginSession(SharedRoom, first);

		Assert.Equal(0, service.GetSessionSummary(first).UniquePlayerCount);
	}

	[Fact]
	public void RecapsRemainAfterLeavingAndExpireAfterADayOfInactivity()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB");
		using ServiceProvider provider = CreateScopeProvider();
		WdfService service = CreateService(maps, provider, clock);
		Guid first = Guid.NewGuid(), second = Guid.NewGuid();
		service.BeginSession(SharedRoom, first);
		service.BeginSession(SharedRoom, second);
		service.Leave(SharedRoom, first);
		service.Leave(SharedRoom, second);
		clock.UtcNow = clock.UtcNow.AddHours(4);
		Assert.Equal(1, service.GetSessionSummary(first).UniquePlayerCount);
		clock.UtcNow = clock.UtcNow.AddHours(21);
		Assert.Equal(0, service.GetSessionSummary(first).UniquePlayerCount);
		Assert.Empty(service.GetSessionSummary(first).Countries);
	}

	[Fact]
	public void VoteWinnerUnlocksTheOfficialRoundTimeline()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB", "MapC");
		using ServiceProvider provider = CreateScopeProvider();
		WdfService service = CreateService(maps, provider, clock);

		WdfScreen vote = Assert.Single(service.GetScreens(SharedRoom, null).Screens);
		WdfVoteScreenInfo voteInfo = Assert.IsType<WdfVoteScreenInfo>(vote.VoteInfo);
		Assert.Null(service.GetCurrentMap(SharedRoom));
		Assert.Equal(2, voteInfo.VoteOptions.Count);
		Assert.True(service.SubmitVote(SharedRoom, Guid.NewGuid(), voteInfo.VoteOptions[1]));

		clock.UtcNow = FromServerTime(voteInfo.VoteResultFetchTime + 0.001);
		WdfVoteResult result = service.GetVoteResult(SharedRoom);
		string winner = result.Entries[0].Name;
		WdfCurrentMap current = Assert.IsType<WdfCurrentMap>(service.GetCurrentMap(SharedRoom));
		WdfScreenList schedule = service.GetScreens(SharedRoom, null);

		Assert.Equal(
			["vote", "vote-lobby", "in-game", "waiting-screen", "vote-recap", "vote"],
			schedule.Screens.Select(screen => screen.Type));
		Assert.Equal(winner, current.MapName);
		Assert.Equal(current.MapName, schedule.Screens[1].MapName);
		Assert.Equal(51, schedule.Screens[0].EndTime - schedule.Screens[0].StartTime);
		Assert.Equal(15, schedule.Screens[1].EndTime - schedule.Screens[1].StartTime);
		Assert.Equal(20, schedule.Screens[3].EndTime - schedule.Screens[3].StartTime);
		Assert.Equal(20, schedule.Screens[4].EndTime - schedule.Screens[4].StartTime);
	}

	[Fact]
	public void VoteIsAcceptedAfterChoiceTimeAndBeforeResultFetchTime()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB");
		WdfDirector rotation = CreateRotation(maps, clock);
		WdfVoteScreenInfo vote = GetVote(rotation, SharedRoom);

		clock.UtcNow = FromServerTime(vote.VoteEndTime + 0.001);

		Assert.True(rotation.SubmitVote(SharedRoom, Guid.NewGuid(), vote.VoteOptions[0]));
	}

	[Fact]
	public void RotationOnlyOffersClassicCompatibleMapsDuringHappyHour()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, OrderedDictionary<Guid, ServerMap> mapEntries) = CreateMaps(
			"CompatibleA", "CompatibleB", "Incompatible");
		Guid incompatibleId = mapEntries.Single(pair => pair.Value.Song.MapName == "Incompatible").Key;
		ServerMap incompatible = mapEntries[incompatibleId];
		mapEntries[incompatibleId] = incompatible with
		{
			Song = incompatible.Song with { Title = "ポーカーフェイス" }
		};
		WdfDirector rotation = CreateRotation(maps, clock);

		WdfVoteScreenInfo vote = GetVote(rotation, SharedRoom);

		Assert.Equal(2, vote.VoteOptions.Count);
		Assert.DoesNotContain("Incompatible", vote.VoteOptions);
		Assert.Throws<ArgumentException>(() => rotation.Enqueue(SharedRoom, "Incompatible"));
	}

	[Fact]
	public void ReplacingQueueControlsTheNextUnstartedRound()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB", "MapC");
		using ServiceProvider provider = CreateScopeProvider();
		WdfDirector director = CreateRotation(maps, clock);
		WdfService service = CreateService(director, provider, clock);

		WdfScreen firstVote = Assert.Single(service.GetScreens(SharedRoom, null).Screens);
		director.ReplaceQueue(SharedRoom, ["MapC"]);
		Assert.Equal(["MapC"], director.GetQueue(SharedRoom));

		WdfVoteScreenInfo firstVoteInfo = Assert.IsType<WdfVoteScreenInfo>(firstVote.VoteInfo);
		clock.UtcNow = FromServerTime(firstVoteInfo.VoteResultFetchTime + 0.001);
		service.GetVoteResult(SharedRoom);
		WdfScreen recap = service.GetScreens(SharedRoom, null).Screens.Single(screen => screen.Type == "vote-recap");
		clock.UtcNow = FromServerTime(recap.EndTime + 0.001);
		WdfVoteScreenInfo queuedInfo = GetVote(service, SharedRoom);
		Assert.Equal(["MapC"], queuedInfo.VoteOptions);
	}

	[Fact]
	public void APlayerCanChangeTheirVoteAndTiesAreResolvedUnambiguously()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB");
		using ServiceProvider provider = CreateScopeProvider();
		WdfService service = CreateService(maps, provider, clock);
		WdfVoteScreenInfo info = GetVote(service, SharedRoom);
		Guid changingPlayer = Guid.NewGuid();

		Assert.True(service.SubmitVote(SharedRoom, changingPlayer, info.VoteOptions[0]));
		Assert.True(service.SubmitVote(SharedRoom, changingPlayer, info.VoteOptions[1]));
		Assert.True(service.SubmitVote(SharedRoom, Guid.NewGuid(), info.VoteOptions[0]));
		Assert.All(service.GetVoteResult(SharedRoom).Entries, entry => Assert.Equal(50, entry.Value));

		clock.UtcNow = FromServerTime(info.VoteResultFetchTime + 0.001);
		WdfVoteResult result = service.GetVoteResult(SharedRoom);
		WdfVoteResultEntry winner = result.Entries[0];
		Assert.Single(result.Entries, entry => entry.Value == winner.Value);
		Assert.Equal(winner.Name, service.GetCurrentMap(SharedRoom)!.MapName);
	}

	[Fact]
	public async Task ScoresStayInTheirRoomAndAlsoWriteSharedMapScores()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("PokerFaceALT", "EsaCola");
		Guid playerId = Guid.NewGuid();
		Profile profile = new()
		{
			Id = playerId,
			Dancercard = new DancerCard { Id = playerId, Name = "Test Dancer" }
		};
		ClassicProfile classic = new()
		{
			ProfileId = playerId,
			Name = "Classic Dancer",
			CountryId = 33,
			AvatarId = 4,
			Platform = "nx"
		};
		Mock<IUserProfileService> profiles = new();
		profiles.Setup(service => service.GetProfileByIdAsync(playerId)).ReturnsAsync(profile);
		Mock<IClassicProfileService> classicProfiles = new();
		classicProfiles.Setup(service => service.GetClassicProfileAsync(playerId)).ReturnsAsync(classic);
		Mock<IScoreService> scores = new();
		scores.Setup(service => service.RecordMapPlayAsync(
			It.IsAny<HighscorePerformance>(), It.IsAny<int>(), playerId, It.IsAny<string>(), "switch", false))
			.ReturnsAsync(new ScoreUpdateResult(true, true));
		scores.Setup(service => service.MergeMapScoreAsync(
			It.IsAny<HighscorePerformance>(), It.IsAny<int>(), 0, playerId, It.IsAny<string>(), "switch", false))
			.ReturnsAsync(new ScoreUpdateResult(true, true));

		using ServiceProvider provider = CreateScopeProvider(profiles.Object, classicProfiles.Object, scores.Object);
		WdfService service = CreateService(maps, provider, clock);
		WdfVoteScreenInfo vote = GetVote(service, SharedRoom);
		Assert.True(service.SubmitVote(SharedRoom, playerId, vote.VoteOptions[0]));
		clock.UtcNow = FromServerTime(vote.VoteResultFetchTime + 0.001);
		service.GetVoteResult(SharedRoom);
		string currentMap = service.GetCurrentMap(SharedRoom)!.MapName;

		WdfInitScoreResult initial = await service.InitializeScoreAsync(SharedRoom, playerId, "switch");
		Assert.Equal(0, initial.Score);
		WdfUpdateScoreResult first = await service.UpdateScoreAsync(SharedRoom, playerId, "switch", 0.5);
		WdfScoreEntry player = Assert.Single(first.ScoreEntries);
		Assert.Equal("Classic Dancer", player.Name);
		Assert.Equal(0.5, player.Score);
		Assert.Equal(1, service.GetActivePlayerCount(SharedRoom));

		await service.UpdateScoreAsync(SharedRoom, playerId, "switch", 0.75);
		scores.Verify(service => service.RecordMapPlayAsync(
			It.IsAny<HighscorePerformance>(), 6666, playerId, currentMap, "switch", false), Times.Once);
		scores.Verify(service => service.MergeMapScoreAsync(
			It.IsAny<HighscorePerformance>(), 10000, 0, playerId, currentMap, "switch", false), Times.Once);
	}

	[Fact]
	public async Task EqualWdfScoresShareCompetitionRank()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB");
		using ServiceProvider provider = CreateScopeProvider();
		WdfService service = CreateService(maps, provider, clock);
		WdfVoteScreenInfo vote = GetVote(service, SharedRoom);
		Assert.True(service.SubmitVote(SharedRoom, Guid.NewGuid(), vote.VoteOptions[0]));
		clock.UtcNow = FromServerTime(vote.VoteResultFetchTime + 0.001);
		service.GetVoteResult(SharedRoom);

		Guid first = Guid.NewGuid();
		Guid second = Guid.NewGuid();
		Guid third = Guid.NewGuid();
		await service.UpdateScoreAsync(SharedRoom, first, "switch", 0.75);
		await service.UpdateScoreAsync(SharedRoom, second, "switch", 0.75);
		await service.UpdateScoreAsync(SharedRoom, third, "switch", 0.5);

		Assert.Equal(1, service.GetRecap(SharedRoom, first).CurrentRank);
		Assert.Equal(1, service.GetRecap(SharedRoom, second).CurrentRank);
		Assert.Equal(3, service.GetRecap(SharedRoom, third).CurrentRank);
	}

	[Fact]
	public async Task ZeroAndFailedWdfSubmissionsDoNotConsumeTheFirstSuccessfulPlay()
	{
		TestTimeProvider clock = CreateClock();
		(IMapService maps, _) = CreateMaps("MapA", "MapB");
		Guid player = Guid.NewGuid();
		Mock<IScoreService> scores = new();
		scores.SetupSequence(value => value.RecordMapPlayAsync(
			It.IsAny<HighscorePerformance>(), It.IsAny<int>(), player, It.IsAny<string>(), "switch", false))
			.ReturnsAsync(new ScoreUpdateResult(false, false))
			.ReturnsAsync(new ScoreUpdateResult(true, true));
		scores.Setup(value => value.MergeMapScoreAsync(
			It.IsAny<HighscorePerformance>(), It.IsAny<int>(), 0, player, It.IsAny<string>(), "switch", false))
			.ReturnsAsync(new ScoreUpdateResult(true, true));
		using ServiceProvider provider = CreateScopeProvider(scores: scores.Object);
		WdfService service = CreateService(maps, provider, clock);
		WdfVoteScreenInfo vote = GetVote(service, SharedRoom);
		service.SubmitVote(SharedRoom, player, vote.VoteOptions[0]);
		clock.UtcNow = FromServerTime(vote.VoteResultFetchTime + 0.001);
		service.GetVoteResult(SharedRoom);
		await service.UpdateScoreAsync(SharedRoom, player, "switch", 0);
		scores.VerifyNoOtherCalls();
		await service.UpdateScoreAsync(SharedRoom, player, "switch", 0.5);
		await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
			service.UpdateScoreAsync(SharedRoom, player, "switch", 0.75).AsTask()));
		scores.Verify(value => value.RecordMapPlayAsync(
			It.IsAny<HighscorePerformance>(), It.IsAny<int>(), player, It.IsAny<string>(), "switch", false), Times.Exactly(2));
		scores.Verify(value => value.MergeMapScoreAsync(
			It.IsAny<HighscorePerformance>(), 10000, 0, player, It.IsAny<string>(), "switch", false), Times.Exactly(4));
	}

	private static WdfVoteScreenInfo GetVote(WdfDirector rotation, WdfRoomId room)
		=> Assert.IsType<WdfVoteScreenInfo>(Assert.Single(rotation.GetScreens(room, null).Screens).VoteInfo);

	private static WdfVoteScreenInfo GetVote(WdfService service, WdfRoomId room)
		=> Assert.IsType<WdfVoteScreenInfo>(Assert.Single(service.GetScreens(room, null).Screens).VoteInfo);

	private static DateTimeOffset FromServerTime(double seconds)
		=> DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Ceiling(seconds * 1000));

	private static TestTimeProvider CreateClock(DateTimeOffset? now = null)
		=> new(now ?? new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero));

	private static WdfHappyHourSchedule CreateSchedule(TestTimeProvider clock)
		=> new(Options.Create(new WdfSettings()), clock);

	private static WdfDirector CreateRotation(IMapService maps, TimeProvider clock)
		=> new(maps, new WdfCatalog(maps), clock, NullLogger<WdfDirector>.Instance);

	private static WdfService CreateService(
		IMapService maps,
		ServiceProvider provider,
		TestTimeProvider clock)
		=> new(
			CreateRotation(maps, clock),
			CreateSchedule(clock),
			provider.GetRequiredService<IServiceScopeFactory>(),
			clock,
			NullLogger<WdfService>.Instance);

	private static WdfService CreateService(
		WdfDirector director,
		ServiceProvider provider,
		TestTimeProvider clock)
		=> new(
			director,
			CreateSchedule(clock),
			provider.GetRequiredService<IServiceScopeFactory>(),
			clock,
			NullLogger<WdfService>.Instance);

	private static (IMapService Service, OrderedDictionary<Guid, ServerMap> Maps) CreateMaps(
		params string[] mapNames)
	{
		OrderedDictionary<Guid, ServerMap> maps = [];
		foreach (string mapName in mapNames)
		{
			Guid songId = Guid.NewGuid();
			JustDanceSongDBEntry song = new()
			{
				MapName = mapName,
				Title = mapName,
				Artist = "Artist",
				MapLength = 120,
				DanceVersionLocId = new OasisTag(new LocalizedString(1, "Classic"))
			};
			maps[songId] = ServerMapTestData.CreateMap(songId, Path.Combine("maps", mapName), song);
		}

		Mock<IMapService> service = new();
		service.SetupGet(value => value.Maps).Returns(maps);
		service.Setup(value => value.GetMap(It.IsAny<string>()))
			.Returns((string mapName) => maps.Values.FirstOrDefault(map =>
				map.Song.MapName.Equals(mapName, StringComparison.OrdinalIgnoreCase)));
		return (service.Object, maps);
	}

	private static ServiceProvider CreateScopeProvider(
		IUserProfileService? profiles = null,
		IClassicProfileService? classicProfiles = null,
		IScoreService? scores = null)
	{
		ServiceCollection services = new();
		services.AddScoped(_ => profiles ?? Mock.Of<IUserProfileService>());
		services.AddScoped(_ => classicProfiles ?? Mock.Of<IClassicProfileService>());
		services.AddScoped(_ => scores ?? Mock.Of<IScoreService>());
		Mock<IPlayerMetadataService> metadata = new();
		metadata.Setup(service => service.GetAsync(It.IsAny<Guid>()))
			.ReturnsAsync((Guid profileId) => new PlayerMetadata { ProfileId = profileId });
		services.AddScoped(_ => metadata.Object);
		return services.BuildServiceProvider();
	}

	private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
	{
		public DateTimeOffset UtcNow { get; set; } = utcNow;
		public override DateTimeOffset GetUtcNow() => UtcNow;
	}
}
