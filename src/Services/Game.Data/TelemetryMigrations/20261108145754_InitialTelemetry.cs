using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JabaSurvivol.Game.Data.TelemetryMigrations
{
    /// <inheritdoc />
    public partial class InitialTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "telemetry");

            migrationBuilder.CreateTable(
                name: "DailyAggregates",
                schema: "telemetry",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    RunCount = table.Column<int>(type: "integer", nullable: false),
                    KillCount = table.Column<int>(type: "integer", nullable: false),
                    BossDefeats = table.Column<int>(type: "integer", nullable: false),
                    LevelUps = table.Column<int>(type: "integer", nullable: false),
                    TotalDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    AverageRunDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    EventCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyAggregates", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryEvents",
                schema: "telemetry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_AccountId_EventId",
                schema: "telemetry",
                table: "TelemetryEvents",
                columns: new[] { "AccountId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_OccurredAt",
                schema: "telemetry",
                table: "TelemetryEvents",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyAggregates",
                schema: "telemetry");

            migrationBuilder.DropTable(
                name: "TelemetryEvents",
                schema: "telemetry");
        }
    }
}
