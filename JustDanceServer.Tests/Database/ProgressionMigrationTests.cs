using JustDanceServer.JustDanceClasses.Database;
using JustDanceServer.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace JustDanceServer.Tests.Database;

public sealed class ProgressionMigrationTests
{
	private const string InitialMigration = "20250406201324_AddPlaylist";

	[Fact]
	public async Task MigrationPreservesJdPlusProgressionAndProfileCountryMetadata()
	{
		await using SqliteConnection connection = new("Data Source=:memory:;Pooling=False");
		await connection.OpenAsync();
		MapIdentityMigrationFunctions.Register(connection, new Dictionary<Guid, string>());
		DbContextOptions<UserDataContext> options = new DbContextOptionsBuilder<UserDataContext>()
			.UseSqlite(connection)
			.Options;
		await using UserDataContext context = new(options);
		IMigrator migrator = context.GetService<IMigrator>();
		await migrator.MigrateAsync(InitialMigration);
		Guid profileId = Guid.NewGuid();
		await InsertLegacyProfileAsync(connection, profileId);

		await migrator.MigrateAsync();

		await using (SqliteCommand command = connection.CreateCommand())
		{
			command.CommandText = """
				SELECT "Family", "Xp", "Level", "Prestige"
				FROM "PlayerProgressions" WHERE "ProfileId" = $profileId;
				""";
			command.Parameters.AddWithValue("$profileId", profileId);
			await using SqliteDataReader reader = await command.ExecuteReaderAsync();
			Assert.True(await reader.ReadAsync());
			Assert.Equal(0, reader.GetInt32(0));
			Assert.Equal(4321, reader.GetInt32(1));
			Assert.Equal(12, reader.GetInt32(2));
			Assert.Equal(3, reader.GetInt32(3));
			Assert.False(await reader.ReadAsync());
		}

		await using (SqliteCommand command = connection.CreateCommand())
		{
			command.CommandText = """
				SELECT "CountryCode", "CountryId", "Locale"
				FROM "PlayerMetadata" WHERE "ProfileId" = $profileId;
				""";
			command.Parameters.AddWithValue("$profileId", profileId);
			await using SqliteDataReader reader = await command.ExecuteReaderAsync();
			Assert.True(await reader.ReadAsync());
			Assert.Equal("FR", reader.GetString(0));
			Assert.Equal(0, reader.GetInt32(1));
			Assert.Equal("en-US", reader.GetString(2));
		}

		await migrator.MigrateAsync(InitialMigration);
		await using (SqliteCommand command = connection.CreateCommand())
		{
			command.CommandText = """
				SELECT "CurrentXP", "CurrentLevel", "PrestigeGrade"
				FROM "Profiles" WHERE "Id" = $profileId;
				""";
			command.Parameters.AddWithValue("$profileId", profileId);
			await using SqliteDataReader reader = await command.ExecuteReaderAsync();
			Assert.True(await reader.ReadAsync());
			Assert.Equal(4321, reader.GetInt32(0));
			Assert.Equal(12, reader.GetInt32(1));
			Assert.Equal(3, reader.GetInt32(2));
		}
	}

	private static async Task InsertLegacyProfileAsync(SqliteConnection connection, Guid profileId)
	{
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = """
			INSERT INTO "DancerCard" (
				"Id", "Name", "Country", "AvatarId", "PortraitBorderId", "AliasId",
				"AliasGender", "BackgroundId", "ScoringFxId", "VictoryFxId", "BadgesIds", "StickersIds")
			VALUES (
				$profileId, 'Player', 'fr', $emptyGuid, $emptyGuid, $emptyGuid,
				'', $emptyGuid, $emptyGuid, $emptyGuid, '[]', '[]');

			INSERT INTO "Profiles" (
				"Id", "Ticket", "DancercardId", "CurrentXP", "CurrentLevel", "PrestigeGrade")
			VALUES ($profileId, $ticket, $profileId, 4321, 12, 3);
			""";
		command.Parameters.AddWithValue("$profileId", profileId);
		command.Parameters.AddWithValue("$emptyGuid", Guid.Empty);
		command.Parameters.AddWithValue("$ticket", $"ticket-{profileId:N}");
		await command.ExecuteNonQueryAsync();
	}
}
