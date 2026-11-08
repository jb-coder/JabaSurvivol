using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JabaSurvivol.Game.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaderboardRuns",
                schema: "game",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    HighestRound = table.Column<int>(type: "integer", nullable: false),
                    TotalKills = table.Column<int>(type: "integer", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ClientVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AchievedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardRuns_AccountId_RunId",
                schema: "game",
                table: "LeaderboardRuns",
                columns: new[] { "AccountId", "RunId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardRuns_HighestRound_TotalKills_DurationSeconds_Ach~",
                schema: "game",
                table: "LeaderboardRuns",
                columns: new[] { "HighestRound", "TotalKills", "DurationSeconds", "AchievedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaderboardRuns",
                schema: "game");
        }
    }
}
