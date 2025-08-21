using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddTMFScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Maps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MapUid = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maps", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TMFCampaigns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMFCampaigns", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TMFLogin",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nickname = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMFLogin", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TMFCampaignScoresSnapshots",
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
                    table.PrimaryKey("PK_TMFCampaignScoresSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TMFCampaignScoresSnapshots_TMFCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "TMFCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TMFCampaignScoresRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SnapshotId = table.Column<int>(type: "int", nullable: false),
                    MapId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<string>(type: "varchar(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMFCampaignScoresRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TMFCampaignScoresRecords_Maps_MapId",
                        column: x => x.MapId,
                        principalTable: "Maps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TMFCampaignScoresRecords_TMFCampaignScoresSnapshots_Snapshot~",
                        column: x => x.SnapshotId,
                        principalTable: "TMFCampaignScoresSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TMFCampaignScoresRecords_TMFLogin_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "TMFLogin",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_MapUid",
                table: "Maps",
                column: "MapUid");

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresRecords_MapId",
                table: "TMFCampaignScoresRecords",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresRecords_PlayerId",
                table: "TMFCampaignScoresRecords",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresRecords_SnapshotId",
                table: "TMFCampaignScoresRecords",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresSnapshots_CampaignId",
                table: "TMFCampaignScoresSnapshots",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresSnapshots_CreatedAt",
                table: "TMFCampaignScoresSnapshots",
                column: "CreatedAt",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TMFCampaignScoresRecords");

            migrationBuilder.DropTable(
                name: "Maps");

            migrationBuilder.DropTable(
                name: "TMFCampaignScoresSnapshots");

            migrationBuilder.DropTable(
                name: "TMFLogin");

            migrationBuilder.DropTable(
                name: "TMFCampaigns");
        }
    }
}
