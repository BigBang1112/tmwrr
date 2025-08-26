using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignScoresPlayerCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TMFCampaignScoresPlayerCounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SnapshotId = table.Column<int>(type: "int", nullable: false),
                    MapId = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMFCampaignScoresPlayerCounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TMFCampaignScoresPlayerCounts_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TMFCampaignScoresPlayerCounts_TMFCampaignScoresSnapshots_Sna~",
                        column: x => x.SnapshotId,
                        principalTable: "TMFCampaignScoresSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresPlayerCounts_MapId",
                table: "TMFCampaignScoresPlayerCounts",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresPlayerCounts_SnapshotId",
                table: "TMFCampaignScoresPlayerCounts",
                column: "SnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TMFCampaignScoresPlayerCounts");
        }
    }
}
