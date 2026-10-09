using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JustDanceServer.Migrations
{
    /// <inheritdoc />
    public partial class SharedDiscoveryActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerMapFavorites",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MapName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "NOCASE"),
                    FavoritedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMapFavorites", x => new { x.ProfileId, x.MapName });
                    table.ForeignKey(
                        name: "FK_PlayerMapFavorites_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMapOrdinals",
                columns: table => new
                {
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceScope = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    MapName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "NOCASE"),
                    Ordinal = table.Column<long>(type: "INTEGER", nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMapOrdinals", x => new { x.ProfileId, x.SourceScope, x.MapName });
                    table.ForeignKey(
                        name: "FK_PlayerMapOrdinals_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMapPlays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MapName = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false, collation: "NOCASE"),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceEventKey = table.Column<string>(type: "TEXT", maxLength: 160, nullable: false),
                    Family = table.Column<int>(type: "INTEGER", nullable: false),
                    GameYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Platform = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PlayedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Score = table.Column<int>(type: "INTEGER", nullable: false),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Attribution = table.Column<int>(type: "INTEGER", nullable: false),
                    Completed = table.Column<bool>(type: "INTEGER", nullable: true),
                    WeeklyEligible = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMapPlays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerMapPlays_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMapOrdinals_ObservedAtUtc",
                table: "PlayerMapOrdinals",
                column: "ObservedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMapPlays_MapName_WeeklyEligible_PlayedAtUtc_Score",
                table: "PlayerMapPlays",
                columns: ["MapName", "WeeklyEligible", "PlayedAtUtc", "Score"]);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMapPlays_PlayedAtUtc",
                table: "PlayerMapPlays",
                column: "PlayedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMapPlays_ProfileId_PlayedAtUtc",
                table: "PlayerMapPlays",
                columns: ["ProfileId", "PlayedAtUtc"]);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerMapPlays_ProfileId_Source_SourceEventKey",
                table: "PlayerMapPlays",
                columns: ["ProfileId", "Source", "SourceEventKey"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerMapFavorites");

            migrationBuilder.DropTable(
                name: "PlayerMapOrdinals");

            migrationBuilder.DropTable(
                name: "PlayerMapPlays");
        }
    }
}
