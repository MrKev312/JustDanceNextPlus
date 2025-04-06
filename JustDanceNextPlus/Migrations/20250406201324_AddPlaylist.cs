using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustDanceNextPlus.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaylist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BossMode",
                columns: table => new
                {
                    BossId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BossMode", x => x.BossId);
                });

            migrationBuilder.CreateTable(
                name: "CompletedTasks",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletedTasks", x => x.TaskId);
                });

            migrationBuilder.CreateTable(
                name: "DancerCard",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Country = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PortraitBorderId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AliasId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AliasGender = table.Column<string>(type: "TEXT", nullable: false),
                    BackgroundId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScoringFxId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VictoryFxId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BadgesIds = table.Column<string>(type: "TEXT", nullable: false),
                    StickersIds = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DancerCard", x => x.Id);
                });

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
                constraints: table =>
                {
                    table.PrimaryKey("PK_HighScores", x => new { x.MapId, x.ProfileId });
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveCompletionData",
                columns: table => new
                {
                    ObjectiveId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveCompletionData", x => x.ObjectiveId);
                });

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
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaylistHighScores", x => new { x.PlaylistId, x.ProfileId });
                });

            migrationBuilder.CreateTable(
                name: "RunningTasks",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    StepsDone = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunningTasks", x => x.TaskId);
                });

            migrationBuilder.CreateTable(
                name: "BossStats",
                columns: table => new
                {
                    BossStatsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BossModeBossId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BossStats", x => x.BossStatsId);
                    table.ForeignKey(
                        name: "FK_BossStats_BossMode_BossModeBossId",
                        column: x => x.BossModeBossId,
                        principalTable: "BossMode",
                        principalColumn: "BossId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Ticket = table.Column<string>(type: "TEXT", nullable: false),
                    DancercardId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurrentXP = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    PrestigeGrade = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_DancerCard_DancercardId",
                        column: x => x.DancercardId,
                        principalTable: "DancerCard",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BossStats_BossModeBossId",
                table: "BossStats",
                column: "BossModeBossId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_DancercardId",
                table: "Profiles",
                column: "DancercardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BossStats");

            migrationBuilder.DropTable(
                name: "CompletedTasks");

            migrationBuilder.DropTable(
                name: "HighScores");

            migrationBuilder.DropTable(
                name: "ObjectiveCompletionData");

            migrationBuilder.DropTable(
                name: "PlaylistHighScores");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "RunningTasks");

            migrationBuilder.DropTable(
                name: "BossMode");

            migrationBuilder.DropTable(
                name: "DancerCard");
        }
    }
}
