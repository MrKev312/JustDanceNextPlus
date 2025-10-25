using JustDanceNextPlus.JustDanceClasses.Database;
using JustDanceNextPlus.JustDanceClasses.Database.Profile;
using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

namespace JustDanceNextPlus.Tests.Services;

public class UserDataServiceTests : IDisposable
{
    private readonly UserDataContext _dbContext;
    private readonly UserDataService _service;
    private readonly DbContextOptions<UserDataContext> _dbOptions;
    private readonly Guid _mapId;
    private readonly Guid _playlistId;
    private readonly Profile _profile1, _profile2, _profile3;

    public UserDataServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<UserDataContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new UserDataContext(_dbOptions);
        _service = new UserDataService(_dbContext, Mock.Of<ILogger<UserDataService>>());

        _mapId = Guid.NewGuid();
        _playlistId = Guid.NewGuid();
        _profile1 = new([]) { Id = Guid.NewGuid(), Dancercard = new() { Name = "Player A" }, Ticket = "ticket-A" };
        _profile2 = new([]) { Id = Guid.NewGuid(), Dancercard = new() { Name = "Player B" } };
        _profile3 = new([]) { Id = Guid.NewGuid(), Dancercard = new() { Name = "Player C" } };

        _dbContext.Profiles.AddRange(_profile1, _profile2, _profile3);
        _dbContext.HighScores.AddRange(
            new MapStats { ProfileId = _profile1.Id, MapId = _mapId, HighScore = 10000, PlayCount = 2 },
            new MapStats { ProfileId = _profile2.Id, MapId = _mapId, HighScore = 12000, PlayCount = 5 },
            new MapStats { ProfileId = _profile3.Id, MapId = _mapId, HighScore = 11000, PlayCount = 1 }
        );
        _dbContext.PlaylistHighScores.AddRange(
            new PlaylistStats { ProfileId = _profile1.Id, PlaylistId = _playlistId, HighScore = 20000, PlayCount = 2 },
            new PlaylistStats { ProfileId = _profile2.Id, PlaylistId = _playlistId, HighScore = 22000, PlayCount = 3 },
            new PlaylistStats { ProfileId = _profile3.Id, PlaylistId = _playlistId, HighScore = 21000, PlayCount = 1 }
        );
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task AddProfileAsync_WithExistingId_ReturnsFalse()
    {
        // Arrange
        Profile duplicateProfile = new([]) { Id = _profile1.Id, Dancercard = new() { Id = _profile1.Id, Name = "Duplicate" } };

        // Act
        bool result = await _service.AddProfileAsync(duplicateProfile);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GenerateAddProfileAsync_CreatesProfileWithNewGuid()
    {
        // Arrange
        Profile newProfile = new([]) { Dancercard = new() { Name = "Newbie" } };

        // Act
        Guid newId = await _service.GenerateAddProfileAsync(newProfile);

        // Assert
        Assert.NotEqual(Guid.Empty, newId);
        Profile? fetchedProfile = await _dbContext.Profiles.FindAsync(newId);
        Assert.NotNull(fetchedProfile);
        Assert.Equal("Newbie", fetchedProfile.Dancercard.Name);
        Assert.Equal(newId, fetchedProfile.Dancercard.Id);
    }

    [Fact]
    public async Task GetHighScoreByProfileIdAndMapIdAsync_ReturnsCorrectScore()
    {
        // Act
        MapStats? score = await _service.GetHighScoreByProfileIdAndMapIdAsync(_profile2.Id, _mapId);

        // Assert
        Assert.NotNull(score);
        Assert.Equal(12000, score.HighScore);
    }

    [Fact]
    public async Task GetHighScoresByProfileIdAsync_ReturnsAllScoresForProfile()
    {
        // Arrange
        Guid anotherMapId = Guid.NewGuid();
        _dbContext.HighScores.Add(new MapStats { ProfileId = _profile1.Id, MapId = anotherMapId, HighScore = 5000 });
        await _dbContext.SaveChangesAsync();

        // Act
        List<MapStats> scores = await _service.GetHighScoresByProfileIdAsync(_profile1.Id);

        // Assert
        Assert.Equal(2, scores.Count);
        Assert.Contains(scores, s => s.MapId == _mapId);
        Assert.Contains(scores, s => s.MapId == anotherMapId);
    }

    [Fact]
    public async Task GetLeaderboardAroundAsync_ReturnsCorrectSliceWithUserInMiddle()
    {
        // Act
        Leaderboard leaderboard = await _service.GetLeaderboardAroundAsync(_mapId, _profile3.Id, limit: 3);

        // Assert
        Assert.Equal(3, leaderboard.Count);
        // Expecting Player B (1st), Player C (2nd), Player A (3rd)
        Assert.Equal(_profile2.Id, leaderboard.Scores[0].ProfileId);
        Assert.Equal(_profile3.Id, leaderboard.Scores[1].ProfileId);
        Assert.Equal(_profile1.Id, leaderboard.Scores[2].ProfileId);
    }

    [Fact]
    public async Task GetLeaderboardByMapIdAsync_ReturnsCorrectlySortedLeaderboard()
    {
        // Act
        Leaderboard leaderboard = await _service.GetLeaderboardByMapIdAsync(_mapId, limit: 3, offset: 0);

        // Assert
        Assert.Equal(3, leaderboard.Count);
        Assert.Equal(_profile2.Id, leaderboard.Scores[0].ProfileId); // Player B - 12000
        Assert.Equal(1, leaderboard.Scores[0].Position);
        Assert.Equal(_profile3.Id, leaderboard.Scores[1].ProfileId); // Player C - 11000
        Assert.Equal(2, leaderboard.Scores[1].Position);
        Assert.Equal(_profile1.Id, leaderboard.Scores[2].ProfileId); // Player A - 10000
        Assert.Equal(3, leaderboard.Scores[2].Position);
    }

    [Fact]
    public async Task GetLeaderboardFromIdsAsync_ReturnsFilteredAndSortedLeaderboard()
    {
        // Arrange
        List<Guid> userIds = [_profile1.Id, _profile3.Id];

        // Act
        Leaderboard leaderboard = await _service.GetLeaderboardFromIdsAsync(_mapId, userIds);

        // Assert
        Assert.Equal(2, leaderboard.Count);
        Assert.Equal(_profile3.Id, leaderboard.Scores[0].ProfileId); // 11000
        Assert.Equal(_profile1.Id, leaderboard.Scores[1].ProfileId); // 10000
    }

    [Fact]
    public async Task GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync_ReturnsCorrectScore()
    {
        // Act
        PlaylistStats? score = await _service.GetPlaylistHighScoreByProfileIdAndPlaylistIdAsync(_profile2.Id, _playlistId);

        // Assert
        Assert.NotNull(score);
        Assert.Equal(22000, score.HighScore);
    }

    [Fact]
    public async Task GetPlaylistHighScoresByProfileIdAsync_ReturnsAllScoresForProfile()
    {
        // Arrange
        Guid anotherPlaylistId = Guid.NewGuid();
        _dbContext.PlaylistHighScores.Add(new PlaylistStats { ProfileId = _profile1.Id, PlaylistId = anotherPlaylistId, HighScore = 5000 });
        await _dbContext.SaveChangesAsync();

        // Act
        List<PlaylistStats> scores = await _service.GetPlaylistHighScoresByProfileIdAsync(_profile1.Id);

        // Assert
        Assert.Equal(2, scores.Count);
        Assert.Contains(scores, s => s.PlaylistId == _playlistId);
        Assert.Contains(scores, s => s.PlaylistId == anotherPlaylistId);
    }

    [Fact]
    public async Task GetPlaylistLeaderboardAroundAsync_ReturnsCorrectSliceWithUserInMiddle()
    {
        // Act
        Leaderboard leaderboard = await _service.GetPlaylistLeaderboardAroundAsync(_playlistId, _profile3.Id, limit: 3);

        // Assert
        Assert.Equal(3, leaderboard.Count);
        // Expecting Player B (1st), Player C (2nd), Player A (3rd)
        Assert.Equal(_profile2.Id, leaderboard.Scores[0].ProfileId);
        Assert.Equal(1, leaderboard.Scores[0].Position);
        Assert.Equal(_profile3.Id, leaderboard.Scores[1].ProfileId);
        Assert.Equal(2, leaderboard.Scores[1].Position);
        Assert.Equal(_profile1.Id, leaderboard.Scores[2].ProfileId);
        Assert.Equal(3, leaderboard.Scores[2].Position);
    }

    [Fact]
    public async Task GetPlaylistLeaderboardByPlaylistIdAsync_ReturnsCorrectlySortedLeaderboard()
    {
        // Act
        Leaderboard leaderboard = await _service.GetPlaylistLeaderboardByPlaylistIdAsync(_playlistId, limit: 3, offset: 0);

        // Assert
        Assert.Equal(3, leaderboard.Count);
        Assert.Equal(_profile2.Id, leaderboard.Scores[0].ProfileId); // 22000
        Assert.Equal(_profile3.Id, leaderboard.Scores[1].ProfileId); // 21000
        Assert.Equal(_profile1.Id, leaderboard.Scores[2].ProfileId); // 20000
    }

    [Fact]
    public async Task GetPlaylistLeaderboardFromIdsAsync_ReturnsFilteredAndSortedLeaderboard()
    {
        // Arrange
        List<Guid> userIds = [_profile1.Id, _profile3.Id];

        // Act
        Leaderboard leaderboard = await _service.GetPlaylistLeaderboardFromIdsAsync(_playlistId, userIds);

        // Assert
        Assert.Equal(2, leaderboard.Count);
        Assert.Equal(_profile3.Id, leaderboard.Scores[0].ProfileId); // 21000
        Assert.Equal(_profile1.Id, leaderboard.Scores[1].ProfileId); // 20000
    }

    [Fact]
    public async Task GetProfileByIdAsync_WhenProfileExists_ReturnsProfileWithStats()
    {
        // Act
        Profile? result = await _service.GetProfileByIdAsync(_profile1.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_profile1.Id, result.Id);
        Assert.Single(result.MapStats);
        Assert.Equal(10000, result.MapStats[_mapId].HighScore);
        Assert.Single(result.PlaylistStats);
        Assert.Equal(20000, result.PlaylistStats[_playlistId].HighScore);
    }

    [Fact]
    public async Task GetProfileByTicketAsync_WhenTicketExists_ReturnsProfile()
    {
        // Act
        Profile? result = await _service.GetProfileByTicketAsync("ticket-A");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_profile1.Id, result.Id);
    }

    [Fact]
    public async Task GetRandomOpponentAsync_WhenOpponentsExist_ReturnsOneOpponent()
    {
        // Act
        Leaderboard opponentLeaderboard = await _service.GetRandomOpponentAsync(_mapId, _profile1.Id);

        // Assert
        Assert.Equal(1, opponentLeaderboard.Count);
        Guid opponentId = opponentLeaderboard.Scores[0].ProfileId;
        Assert.NotEqual(_profile1.Id, opponentId);
        Assert.Contains(opponentId, new[] { _profile2.Id, _profile3.Id });
    }

    [Fact]
    public async Task HasJustDanceProfileAsync_ReturnsCorrectBoolean()
    {
        // Act
        bool shouldBeTrue = await _service.HasJustDanceProfileAsync(_profile1.Id);
        bool shouldBeFalse = await _service.HasJustDanceProfileAsync(Guid.NewGuid());

        // Assert
        Assert.True(shouldBeTrue);
        Assert.False(shouldBeFalse);
    }

    [Fact]
    public async Task UpdateHighScoreAsync_AddsNewScore_WhenNoneExists()
    {
        // Arrange
        Guid newPlayerId = Guid.NewGuid();
        Guid newMapId = Guid.NewGuid();
        HighscorePerformance performance = new();

        // Act
        (bool success, bool isNewHighScore) = await _service.UpdateHighScoreAsync(performance, 13000, newPlayerId, newMapId);

        // Assert
        Assert.True(success);
        Assert.True(isNewHighScore);
        MapStats? savedScore = await _dbContext.HighScores.SingleOrDefaultAsync(hs => hs.ProfileId == newPlayerId && hs.MapId == newMapId);
        Assert.NotNull(savedScore);
        Assert.Equal(13000, savedScore.HighScore);
        Assert.Equal(1, savedScore.PlayCount);
    }

    [Fact]
    public async Task UpdateHighScoreAsync_UpdatesExistingHigherScore()
    {
        // Arrange
        HighscorePerformance performance = new();

        // Act
        (bool success, bool isNewHighScore) = await _service.UpdateHighScoreAsync(performance, 10500, _profile1.Id, _mapId);

        // Assert
        Assert.True(success);
        Assert.True(isNewHighScore);
        MapStats? updatedScore = await _dbContext.HighScores
            .FirstOrDefaultAsync(hs => hs.ProfileId == _profile1.Id && hs.MapId == _mapId);
        Assert.NotNull(updatedScore);
        Assert.Equal(10500, updatedScore.HighScore);
        Assert.Equal(3, updatedScore.PlayCount); // Play count should increment
    }

    [Fact]
    public async Task UpdateHighScoreAsync_DoesNotUpdateForLowerScoreButIncrementsPlayCount()
    {
        // Arrange
        HighscorePerformance performance = new();

        // Act
        (bool success, bool isNewHighScore) = await _service.UpdateHighScoreAsync(performance, 9500, _profile1.Id, _mapId);

        // Assert
        Assert.True(success);
        Assert.False(isNewHighScore);
        MapStats? updatedScore = await _dbContext.HighScores
            .FirstOrDefaultAsync(hs => hs.ProfileId == _profile1.Id && hs.MapId == _mapId);
        Assert.NotNull(updatedScore);
        Assert.Equal(10000, updatedScore.HighScore); // Score should NOT change
        Assert.Equal(3, updatedScore.PlayCount); // Play count SHOULD increment
    }

    [Fact]
    public async Task UpdatePlaylistHighScoreAsync_AddsNewScore_WhenNoneExists()
    {
        // Arrange
        Guid newPlayerId = Guid.NewGuid();
        Guid newPlaylistId = Guid.NewGuid();

        // Act
        (bool success, bool isNewHighScore) = await _service.UpdatePlaylistHighScoreAsync(new PlaylistStats(), 25000, newPlayerId, newPlaylistId);

        // Assert
        Assert.True(success);
        Assert.True(isNewHighScore);
        PlaylistStats? savedScore = await _dbContext.PlaylistHighScores
            .SingleOrDefaultAsync(hs => hs.ProfileId == newPlayerId && hs.PlaylistId == newPlaylistId);
        Assert.NotNull(savedScore);
        Assert.Equal(25000, savedScore.HighScore);
        Assert.Equal(1, savedScore.PlayCount);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenProfileExists_UpdatesAndSavesChanges()
    {
        // Arrange
        _profile1.Dancercard.Name = "Updated Name";

        // Act
        bool result = await _service.UpdateProfileAsync(_profile1);

        // Assert
        Assert.True(result);
        await using UserDataContext assertContext = new(_dbOptions);
        Profile? updatedProfile = await assertContext.Profiles
            .Include(p => p.Dancercard)
            .FirstOrDefaultAsync(p => p.Id == _profile1.Id);
        Assert.NotNull(updatedProfile);
        Assert.Equal("Updated Name", updatedProfile.Dancercard.Name);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}