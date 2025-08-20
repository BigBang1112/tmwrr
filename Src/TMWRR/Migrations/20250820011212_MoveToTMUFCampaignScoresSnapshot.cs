using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class MoveToTMUFCampaignScoresSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TMUFScoresSnapshots");

            migrationBuilder.CreateTable(
                name: "TMUFCampaignScoresSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CampaignId = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMUFCampaignScoresSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TMUFCampaignScoresSnapshots_TMUFCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "TMUFCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TMUFCampaignScoresSnapshots_CampaignId",
                table: "TMUFCampaignScoresSnapshots",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_TMUFCampaignScoresSnapshots_CreatedAt",
                table: "TMUFCampaignScoresSnapshots",
                column: "CreatedAt",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TMUFCampaignScoresSnapshots");

            migrationBuilder.CreateTable(
                name: "TMUFScoresSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CampaignId = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMUFScoresSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TMUFScoresSnapshots_TMUFCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "TMUFCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TMUFScoresSnapshots_CampaignId",
                table: "TMUFScoresSnapshots",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_TMUFScoresSnapshots_CreatedAt",
                table: "TMUFScoresSnapshots",
                column: "CreatedAt",
                unique: true);
        }
    }
}
