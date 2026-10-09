using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using System.Text.Json;

namespace JustDanceServer.Tests.Database;

public sealed class MapNameScoreMigrationTests : IDisposable
{
	private const string InitialMigration = "20250406201324_AddPlaylist";
	private readonly string tempDirectory = Path.Combine(
		Path.GetTempPath(),
		$"jdnp-map-score-migration-{Guid.NewGuid():N}");

	[Fact]
	public async Task MigrationPreservesExactMapNamesAndDropsMissingIds()
	{
		Directory.CreateDirectory(tempDirectory);
		Guid songId = Guid.Parse("f3f70415-f652-4679-895b-f8d60d197d64");
		Guid missingSongId = Guid.Parse("f3f70415-f652-4679-895b-f8d60d197d65");
		Guid profileId = Guid.NewGuid();
		Guid playlistId = Guid.NewGuid();
		await using SqliteConnection connection = new(
			$"Data Source={Path.Combine(tempDirectory, "userdata.db")};Pooling=False");
		await connection.OpenAsync();
		DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
			.UseSqlite(connection)
			.Options;
		await using UserDataContext context = new(options);
		IMigrator migrator = context.GetService<IMigrator>();
		await migrator.MigrateAsync(InitialMigration);

		await InsertLegacyScoresAsync(connection, songId, missingSongId, profileId, playlistId);
		MapIdentityMigrationFunctions.Register(
			connection,
			new Dictionary<Guid, string> { [songId] = "PokerFaceALT" });
		await migrator.MigrateAsync();

		await using (SqliteCommand command = connection.CreateCommand())
		{
			command.CommandText = """
				SELECT "MapName", "HighScore", "PlayCount", "Platform",
					json_extract("HighScorePerformance", '$.Moves.Super'),
					json_extract("GameModeStats", '$.Challenge.LastScore')
				FROM "MapScores";
				""";
			await using SqliteDataReader reader = await command.ExecuteReaderAsync();
			Assert.True(await reader.ReadAsync());
			Assert.Equal("PokerFaceALT", reader.GetString(0));
			Assert.Equal(12345, reader.GetInt32(1));
			Assert.Equal(7, reader.GetInt32(2));
			Assert.Equal("switch", reader.GetString(3));
			Assert.Equal(4, reader.GetInt32(4));
			Assert.Equal(321, reader.GetInt32(5));
			Assert.False(await reader.ReadAsync());
		}

		await using (SqliteCommand command = connection.CreateCommand())
		{
			command.CommandText = "SELECT \"HighScorePerMap\" FROM \"PlaylistScores\";";
			string json = Assert.IsType<string>(await command.ExecuteScalarAsync());
			Dictionary<string, int> scores = JsonSerializer.Deserialize<Dictionary<string, int>>(json)!;
			Assert.Equal(new Dictionary<string, int> { ["PokerFaceALT"] = 12345 }, scores);
		}

		await migrator.MigrateAsync(InitialMigration);
		await using (SqliteCommand command = connection.CreateCommand())
		{
			command.CommandText = "SELECT \"MapId\" FROM \"HighScores\";";
			Assert.Equal(songId.ToString(), await command.ExecuteScalarAsync());
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InitializerBacksUpCommittedWalBeforeDeletingUnknownMapsOrStopsIfBackupFails(bool blockBackup)
	{
		Directory.CreateDirectory(tempDirectory);
		string databasePath = Path.Combine(tempDirectory, "legacy.db");
		string connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = databasePath, Pooling = false
		}.ToString();
		await using SqliteConnection connection = new(connectionString);
		await connection.OpenAsync();
		await using UserDataContext setup = new(new DbContextOptionsBuilder<UserDataContext>()
			.UseSqlite(connection).Options);
		await setup.GetService<IMigrator>().MigrateAsync(InitialMigration);
		await setup.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
		await setup.Database.ExecuteSqlRawAsync("PRAGMA wal_autocheckpoint=0;");
		await InsertLegacyScoresAsync(connection, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
		Assert.True(new FileInfo(databasePath + "-wal").Length > 32);
		if (blockBackup)
			await File.WriteAllTextAsync(Path.Combine(tempDirectory, "backups"), "block directory creation");

		ServiceCollection services = new();
		services.AddDbContext<UserDataContext>(options => options.UseSqlite(connectionString));
		await using ServiceProvider provider = services.BuildServiceProvider();
		Mock<IMapService> maps = new();
		maps.SetupGet(service => service.Maps).Returns([]);
		DatabaseInitializer initializer = new(provider.GetRequiredService<IServiceScopeFactory>(),
			maps.Object, NullLogger<DatabaseInitializer>.Instance);
		if (blockBackup)
		{
			await Assert.ThrowsAsync<IOException>(() => initializer.InitializeAsync(CancellationToken.None));
			Assert.Contains(await setup.Database.GetPendingMigrationsAsync(), name => name != InitialMigration);
			await using SqliteCommand command = connection.CreateCommand();
			command.CommandText = "SELECT COUNT(*) FROM HighScores";
			Assert.Equal(2L, await command.ExecuteScalarAsync());
			return;
		}

		await initializer.InitializeAsync(CancellationToken.None);
		Assert.Empty(await setup.Database.GetPendingMigrationsAsync());
		Assert.Empty(await setup.MapScores.ToListAsync());
		string backupPath = Assert.Single(Directory.GetFiles(Path.Combine(tempDirectory, "backups"), "*.db"));
		await using SqliteConnection backup = new(new SqliteConnectionStringBuilder
		{
			DataSource = backupPath, Pooling = false, Mode = SqliteOpenMode.ReadOnly
		}.ToString());
		await backup.OpenAsync();
		await using SqliteCommand verify = backup.CreateCommand();
		verify.CommandText = "SELECT COUNT(*) FROM HighScores";
		Assert.Equal(2L, await verify.ExecuteScalarAsync());
		verify.CommandText = "SELECT HighScore FROM HighScores ORDER BY HighScore DESC LIMIT 1";
		Assert.Equal(13000L, await verify.ExecuteScalarAsync());
		verify.CommandText = "SELECT COUNT(*) FROM PlaylistHighScores";
		Assert.Equal(1L, await verify.ExecuteScalarAsync());
	}

	private static async Task InsertLegacyScoresAsync(
		SqliteConnection connection,
		Guid songId,
		Guid missingSongId,
		Guid profileId,
		Guid playlistId)
	{
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = """
			INSERT INTO "HighScores" (
				"MapId", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt",
				"HighScorePerformance_GoldMovesAchieved", "HighScorePerformance_Moves_Missed",
				"HighScorePerformance_Moves_Okay", "HighScorePerformance_Moves_Good",
				"HighScorePerformance_Moves_Super", "HighScorePerformance_Moves_Perfect",
				"HighScorePerformance_Moves_Gold", "GameModeStats_Exists",
				"GameModeStats_Challenge_LastScore")
			VALUES ($mapId, $profileId, 12345, 7, 'switch', '2026-08-09T12:00:00.0000000Z',
				'[true,false]', 1, 2, 3, 4, 5, 6, 1, 321);

			INSERT INTO "HighScores" (
				"MapId", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt",
				"HighScorePerformance_GoldMovesAchieved", "HighScorePerformance_Moves_Missed",
				"HighScorePerformance_Moves_Okay", "HighScorePerformance_Moves_Good",
				"HighScorePerformance_Moves_Super", "HighScorePerformance_Moves_Perfect",
				"HighScorePerformance_Moves_Gold", "GameModeStats_Exists",
				"GameModeStats_Challenge_LastScore")
			VALUES ($missingSongId, $profileId, 13000, 2, 'switch', '2026-08-09T13:00:00.0000000Z',
				'[true,true]', 2, 3, 4, 8, 6, 7, 1, 999);

			INSERT INTO "PlaylistHighScores" (
				"PlaylistId", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt", "HighScorePerMap")
			VALUES ($playlistId, $profileId, 12345, 2, 'switch', '2026-08-09T12:00:00.0000000Z', $playlistScores);
			""";
		command.Parameters.AddWithValue("$mapId", songId);
		command.Parameters.AddWithValue("$missingSongId", missingSongId);
		command.Parameters.AddWithValue("$profileId", profileId);
		command.Parameters.AddWithValue("$playlistId", playlistId);
		command.Parameters.AddWithValue(
			"$playlistScores",
			JsonSerializer.Serialize(new Dictionary<Guid, int>
			{
				[songId] = 12345,
				[missingSongId] = 13000
			}));
		await command.ExecuteNonQueryAsync();
	}

	public void Dispose()
	{
		if (Directory.Exists(tempDirectory))
			Directory.Delete(tempDirectory, recursive: true);
	}
}
