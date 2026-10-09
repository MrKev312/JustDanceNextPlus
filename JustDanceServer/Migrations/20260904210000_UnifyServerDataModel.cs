using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustDanceServer.Migrations;

/// <summary>
/// Migrates the upstream database into the unified JDNext/JDU data model.
/// Protocol-specific JDNext song GUIDs become canonical map names; scores without an exact
/// current map match are discarded. The SQLite mapping functions receive identities from the
/// already-loaded map catalog.
/// </summary>
public partial class UnifyServerDataModel : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.RenameTable(
			name: "HighScores",
			newName: "LegacyHighScores");

		migrationBuilder.RenameTable(
			name: "PlaylistHighScores",
			newName: "LegacyPlaylistScores");

		migrationBuilder.CreateTable(
			name: "MapScores",
			columns: table => new
			{
				MapName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "NOCASE"),
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				HighScore = table.Column<int>(type: "INTEGER", nullable: false),
				PlayCount = table.Column<int>(type: "INTEGER", nullable: false),
				Platform = table.Column<string>(type: "TEXT", nullable: false),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
				IsCoopHighScore = table.Column<bool>(type: "INTEGER", nullable: false),
				HighScorePerformance = table.Column<string>(type: "TEXT", nullable: false),
				GameModeStats = table.Column<string>(type: "TEXT", nullable: true)
			},
			constraints: table => table.PrimaryKey("PK_MapScores", x => new { x.MapName, x.ProfileId }));

		migrationBuilder.Sql(
			"""
			INSERT INTO "MapScores" (
				"MapName", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt",
				"IsCoopHighScore", "HighScorePerformance", "GameModeStats")
			SELECT
				jd_map_name("MapId"),
				"ProfileId",
				"HighScore",
				"PlayCount",
				"Platform",
				"UpdatedAt",
				0,
				json_object(
					'GoldMovesAchieved', json(COALESCE("HighScorePerformance_GoldMovesAchieved", '[]')),
					'Moves', json_object(
						'Missed', "HighScorePerformance_Moves_Missed",
						'Okay', "HighScorePerformance_Moves_Okay",
						'Good', "HighScorePerformance_Moves_Good",
						'Super', "HighScorePerformance_Moves_Super",
						'Perfect', "HighScorePerformance_Moves_Perfect",
						'Gold', "HighScorePerformance_Moves_Gold")),
				CASE WHEN "GameModeStats_Exists" IS NULL THEN NULL ELSE
					json_object('Challenge', json_object(
						'LastScore', COALESCE("GameModeStats_Challenge_LastScore", 0)))
				END
			FROM "LegacyHighScores"
			WHERE jd_map_name("MapId") IS NOT NULL;
			""");

		migrationBuilder.CreateTable(
			name: "PlaylistScores",
			columns: table => new
			{
				PlaylistId = table.Column<Guid>(type: "TEXT", nullable: false),
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				HighScore = table.Column<int>(type: "INTEGER", nullable: false),
				PlayCount = table.Column<int>(type: "INTEGER", nullable: false),
				Platform = table.Column<string>(type: "TEXT", nullable: false),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
				HighScorePerMap = table.Column<string>(type: "TEXT", nullable: false)
			},
			constraints: table => table.PrimaryKey("PK_PlaylistScores", x => new { x.PlaylistId, x.ProfileId }));

		migrationBuilder.Sql(
			"""
			INSERT INTO "PlaylistScores" (
				"PlaylistId", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt", "HighScorePerMap")
			SELECT
				"PlaylistId", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt",
				jd_map_score_names("HighScorePerMap")
			FROM "LegacyPlaylistScores";
			""");

		migrationBuilder.DropTable(name: "LegacyHighScores");
		migrationBuilder.DropTable(name: "LegacyPlaylistScores");

		migrationBuilder.CreateTable(
			name: "ClassicProfiles",
			columns: table => new
			{
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				Name = table.Column<string>(type: "TEXT", nullable: false),
				Nickname = table.Column<string>(type: "TEXT", nullable: false),
				AvatarId = table.Column<uint>(type: "INTEGER", nullable: false),
				CountryId = table.Column<int>(type: "INTEGER", nullable: false),
				SkinId = table.Column<uint>(type: "INTEGER", nullable: false),
				AliasId = table.Column<uint>(type: "INTEGER", nullable: false),
				AliasGender = table.Column<int>(type: "INTEGER", nullable: false),
				PortraitBorderId = table.Column<uint>(type: "INTEGER", nullable: false),
				JdPoints = table.Column<int>(type: "INTEGER", nullable: false),
				PlatformId = table.Column<string>(type: "TEXT", nullable: false),
				Platform = table.Column<string>(type: "TEXT", nullable: false),
				PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_ClassicProfiles", x => x.ProfileId);
				table.ForeignKey(
					name: "FK_ClassicProfiles_Profiles_ProfileId",
					column: x => x.ProfileId,
					principalTable: "Profiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateIndex(
			name: "IX_Profiles_Ticket",
			table: "Profiles",
			column: "Ticket",
			unique: true,
			filter: "\"Ticket\" <> ''");

		migrationBuilder.CreateIndex(
			name: "IX_MapScores_Leaderboard",
			table: "MapScores",
			columns: ["MapName", "HighScore", "UpdatedAt", "ProfileId"]);

		migrationBuilder.CreateIndex(
			name: "IX_MapScores_ProfileId",
			table: "MapScores",
			column: "ProfileId");

		migrationBuilder.CreateIndex(
			name: "IX_PlaylistScores_Leaderboard",
			table: "PlaylistScores",
			columns: ["PlaylistId", "HighScore", "UpdatedAt", "ProfileId"]);

		migrationBuilder.CreateIndex(
			name: "IX_PlaylistScores_ProfileId",
			table: "PlaylistScores",
			column: "ProfileId");

		migrationBuilder.CreateIndex(
			name: "IX_ClassicProfiles_CountryId",
			table: "ClassicProfiles",
			column: "CountryId");

		migrationBuilder.CreateTable(
			name: "PlatformIdentities",
			columns: table => new
			{
				Provider = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
				Subject = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				PlatformType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
				IdOnPlatform = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
				NameOnPlatform = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PlatformIdentities", x => new { x.Provider, x.Subject });
				table.ForeignKey(
					name: "FK_PlatformIdentities_Profiles_ProfileId",
					column: x => x.ProfileId,
					principalTable: "Profiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "Friendships",
			columns: table => new
			{
				FirstProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				SecondProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				RequestedByProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				State = table.Column<int>(type: "INTEGER", nullable: false),
				CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_Friendships", x => new { x.FirstProfileId, x.SecondProfileId });
				table.ForeignKey(
					name: "FK_Friendships_Profiles_FirstProfileId",
					column: x => x.FirstProfileId,
					principalTable: "Profiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
				table.ForeignKey(
					name: "FK_Friendships_Profiles_SecondProfileId",
					column: x => x.SecondProfileId,
					principalTable: "Profiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateIndex(
			name: "IX_PlatformIdentities_PlatformType_IdOnPlatform",
			table: "PlatformIdentities",
			columns: ["PlatformType", "IdOnPlatform"]);

		migrationBuilder.CreateIndex(
			name: "IX_PlatformIdentities_PlatformType_NameOnPlatform",
			table: "PlatformIdentities",
			columns: ["PlatformType", "NameOnPlatform"]);

		migrationBuilder.CreateIndex(
			name: "IX_PlatformIdentities_ProfileId",
			table: "PlatformIdentities",
			column: "ProfileId");

		migrationBuilder.CreateIndex(
			name: "IX_Friendships_SecondProfileId",
			table: "Friendships",
			column: "SecondProfileId");

		migrationBuilder.CreateIndex(
			name: "IX_Friendships_State_FirstProfileId",
			table: "Friendships",
			columns: ["State", "FirstProfileId"]);

		migrationBuilder.CreateIndex(
			name: "IX_Friendships_State_SecondProfileId",
			table: "Friendships",
			columns: ["State", "SecondProfileId"]);

		migrationBuilder.CreateTable(
			name: "PlayerGachaStates",
			columns: table => new
			{
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				Family = table.Column<int>(type: "INTEGER", nullable: false),
				Rank = table.Column<int>(type: "INTEGER", nullable: false),
				PullCount = table.Column<int>(type: "INTEGER", nullable: false),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PlayerGachaStates", x => new { x.ProfileId, x.Family });
				table.ForeignKey(
					name: "FK_PlayerGachaStates_Profiles_ProfileId",
					column: x => x.ProfileId,
					principalTable: "Profiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "PlayerMetadata",
			columns: table => new
			{
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				CountryCode = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
				CountryId = table.Column<int>(type: "INTEGER", nullable: false),
				Locale = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
				LastSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PlayerMetadata", x => x.ProfileId);
				table.ForeignKey(
					name: "FK_PlayerMetadata_Profiles_ProfileId",
					column: x => x.ProfileId,
					principalTable: "Profiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "PlayerProgressions",
			columns: table => new
			{
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				Family = table.Column<int>(type: "INTEGER", nullable: false),
				Xp = table.Column<int>(type: "INTEGER", nullable: false),
				Level = table.Column<int>(type: "INTEGER", nullable: false),
				Prestige = table.Column<int>(type: "INTEGER", nullable: false),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PlayerProgressions", x => new { x.ProfileId, x.Family });
				table.ForeignKey(
					name: "FK_PlayerProgressions_Profiles_ProfileId",
					column: x => x.ProfileId,
					principalTable: "Profiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "PlayerQuestProgress",
			columns: table => new
			{
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				Family = table.Column<int>(type: "INTEGER", nullable: false),
				QuestKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
				Progress = table.Column<int>(type: "INTEGER", nullable: false),
				CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
				ClaimedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PlayerQuestProgress", x => new { x.ProfileId, x.Family, x.QuestKey });
				table.ForeignKey(
					name: "FK_PlayerQuestProgress_Profiles_ProfileId",
					column: x => x.ProfileId,
					principalTable: "Profiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateTable(
			name: "PlayerUnlocks",
			columns: table => new
			{
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				Family = table.Column<int>(type: "INTEGER", nullable: false),
				UnlockKey = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
				Source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
				AcquiredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
			},
			constraints: table =>
			{
				table.PrimaryKey("PK_PlayerUnlocks", x => new { x.ProfileId, x.Family, x.UnlockKey });
				table.ForeignKey(
					name: "FK_PlayerUnlocks_Profiles_ProfileId",
					column: x => x.ProfileId,
					principalTable: "Profiles",
					principalColumn: "Id",
					onDelete: ReferentialAction.Cascade);
			});

		migrationBuilder.CreateIndex(
			name: "IX_PlayerMetadata_CountryId",
			table: "PlayerMetadata",
			column: "CountryId");

		migrationBuilder.CreateIndex(
			name: "IX_PlayerMetadata_LastSeenAt",
			table: "PlayerMetadata",
			column: "LastSeenAt");

		migrationBuilder.Sql(
			"""
			INSERT INTO "PlayerProgressions"
				("ProfileId", "Family", "Xp", "Level", "Prestige", "UpdatedAt")
			SELECT
				"Id", 0, MAX("CurrentXP", 0), MAX("CurrentLevel", 1),
				MAX("PrestigeGrade", 0), CURRENT_TIMESTAMP
			FROM "Profiles";
			""");

		migrationBuilder.Sql(
			"""
			INSERT INTO "PlayerMetadata"
				("ProfileId", "CountryCode", "CountryId", "Locale", "LastSeenAt")
			SELECT
				p."Id",
				CASE WHEN length(trim(d."Country")) = 2 THEN upper(trim(d."Country")) ELSE 'NL' END,
				0,
				'en-US',
				CURRENT_TIMESTAMP
			FROM "Profiles" p
			JOIN "DancerCard" d ON d."Id" = p."DancercardId";
			""");

		migrationBuilder.DropColumn(
			name: "CurrentLevel",
			table: "Profiles");

		migrationBuilder.DropColumn(
			name: "CurrentXP",
			table: "Profiles");

		migrationBuilder.DropColumn(
			name: "PrestigeGrade",
			table: "Profiles");
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<int>(
			name: "CurrentLevel",
			table: "Profiles",
			type: "INTEGER",
			nullable: false,
			defaultValue: 0);

		migrationBuilder.AddColumn<int>(
			name: "CurrentXP",
			table: "Profiles",
			type: "INTEGER",
			nullable: false,
			defaultValue: 0);

		migrationBuilder.AddColumn<int>(
			name: "PrestigeGrade",
			table: "Profiles",
			type: "INTEGER",
			nullable: false,
			defaultValue: 0);

		migrationBuilder.Sql(
			"""
			UPDATE "Profiles"
			SET
				"CurrentXP" = COALESCE((
					SELECT "Xp" FROM "PlayerProgressions" value
					WHERE value."ProfileId" = "Profiles"."Id" AND value."Family" = 0), 1),
				"CurrentLevel" = COALESCE((
					SELECT "Level" FROM "PlayerProgressions" value
					WHERE value."ProfileId" = "Profiles"."Id" AND value."Family" = 0), 1),
				"PrestigeGrade" = COALESCE((
					SELECT "Prestige" FROM "PlayerProgressions" value
					WHERE value."ProfileId" = "Profiles"."Id" AND value."Family" = 0), 0);
			""");

		migrationBuilder.DropTable(name: "PlayerGachaStates");
		migrationBuilder.DropTable(name: "PlayerMetadata");
		migrationBuilder.DropTable(name: "PlayerProgressions");
		migrationBuilder.DropTable(name: "PlayerQuestProgress");
		migrationBuilder.DropTable(name: "PlayerUnlocks");
		migrationBuilder.DropTable(name: "PlatformIdentities");
		migrationBuilder.DropTable(name: "Friendships");
		migrationBuilder.DropTable(name: "ClassicProfiles");
		migrationBuilder.DropIndex(name: "IX_Profiles_Ticket", table: "Profiles");

		migrationBuilder.RenameTable(name: "MapScores", newName: "CurrentMapScores");
		migrationBuilder.RenameTable(name: "PlaylistScores", newName: "CurrentPlaylistScores");

		migrationBuilder.CreateTable(
			name: "HighScores",
			columns: table => new
			{
				MapId = table.Column<Guid>(type: "TEXT", nullable: false),
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				HighScore = table.Column<int>(type: "INTEGER", nullable: false),
				PlayCount = table.Column<int>(type: "INTEGER", nullable: false),
				Platform = table.Column<string>(type: "TEXT", nullable: false),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
				HighScorePerformance_GoldMovesAchieved = table.Column<string>(type: "TEXT", nullable: false),
				HighScorePerformance_Moves_Missed = table.Column<int>(type: "INTEGER", nullable: false),
				HighScorePerformance_Moves_Okay = table.Column<int>(type: "INTEGER", nullable: false),
				HighScorePerformance_Moves_Good = table.Column<int>(type: "INTEGER", nullable: false),
				HighScorePerformance_Moves_Super = table.Column<int>(type: "INTEGER", nullable: false),
				HighScorePerformance_Moves_Perfect = table.Column<int>(type: "INTEGER", nullable: false),
				HighScorePerformance_Moves_Gold = table.Column<int>(type: "INTEGER", nullable: false),
				GameModeStats_Exists = table.Column<bool>(type: "INTEGER", nullable: true),
				GameModeStats_Challenge_LastScore = table.Column<int>(type: "INTEGER", nullable: true)
			},
			constraints: table => table.PrimaryKey("PK_HighScores", x => new { x.MapId, x.ProfileId }));

		migrationBuilder.Sql(
			"""
			INSERT INTO "HighScores" (
				"MapId", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt",
				"HighScorePerformance_GoldMovesAchieved", "HighScorePerformance_Moves_Missed",
				"HighScorePerformance_Moves_Okay", "HighScorePerformance_Moves_Good",
				"HighScorePerformance_Moves_Super", "HighScorePerformance_Moves_Perfect",
				"HighScorePerformance_Moves_Gold", "GameModeStats_Exists",
				"GameModeStats_Challenge_LastScore")
			SELECT
				jd_song_id("MapName"), "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt",
				COALESCE(json_extract("HighScorePerformance", '$.GoldMovesAchieved'), '[]'),
				COALESCE(json_extract("HighScorePerformance", '$.Moves.Missed'), 0),
				COALESCE(json_extract("HighScorePerformance", '$.Moves.Okay'), 0),
				COALESCE(json_extract("HighScorePerformance", '$.Moves.Good'), 0),
				COALESCE(json_extract("HighScorePerformance", '$.Moves.Super'), 0),
				COALESCE(json_extract("HighScorePerformance", '$.Moves.Perfect'), 0),
				COALESCE(json_extract("HighScorePerformance", '$.Moves.Gold'), 0),
				CASE WHEN "GameModeStats" IS NULL THEN NULL ELSE 1 END,
				json_extract("GameModeStats", '$.Challenge.LastScore')
			FROM "CurrentMapScores";
			""");

		migrationBuilder.CreateTable(
			name: "PlaylistHighScores",
			columns: table => new
			{
				PlaylistId = table.Column<Guid>(type: "TEXT", nullable: false),
				ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
				HighScore = table.Column<int>(type: "INTEGER", nullable: false),
				PlayCount = table.Column<int>(type: "INTEGER", nullable: false),
				Platform = table.Column<string>(type: "TEXT", nullable: false),
				UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
				HighScorePerMap = table.Column<string>(type: "TEXT", nullable: false)
			},
			constraints: table => table.PrimaryKey("PK_PlaylistHighScores", x => new { x.PlaylistId, x.ProfileId }));

		migrationBuilder.Sql(
			"""
			INSERT INTO "PlaylistHighScores" (
				"PlaylistId", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt", "HighScorePerMap")
			SELECT
				"PlaylistId", "ProfileId", "HighScore", "PlayCount", "Platform", "UpdatedAt",
				jd_map_score_ids("HighScorePerMap")
			FROM "CurrentPlaylistScores";
			""");

		migrationBuilder.DropTable(name: "CurrentMapScores");
		migrationBuilder.DropTable(name: "CurrentPlaylistScores");
	}
}
