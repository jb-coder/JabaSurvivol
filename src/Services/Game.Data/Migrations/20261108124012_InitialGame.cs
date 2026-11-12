using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JabaSurvivol.Game.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "game");

            migrationBuilder.CreateTable(
                name: "CloudSaves",
                schema: "game",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    SchemaVersion = table.Column<int>(type: "integer", nullable: false),
                    Document = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudSaves", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CloudSaves_AccountId_Slot",
                schema: "game",
                table: "CloudSaves",
                columns: new[] { "AccountId", "Slot" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CloudSaves",
                schema: "game");
        }
    }
}
