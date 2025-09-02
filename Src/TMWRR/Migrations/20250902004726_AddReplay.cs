using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddReplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplayId",
                table: "TMFCampaignScoresRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReplayGhostId",
                table: "GhostCheckpoints",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Replays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Data = table.Column<byte[]>(type: "mediumblob", nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Etag = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Replays", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReplayGhosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReplayId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplayGhosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReplayGhosts_Replays_ReplayId",
                        column: x => x.ReplayId,
                        principalTable: "Replays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresRecords_ReplayId",
                table: "TMFCampaignScoresRecords",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_GhostCheckpoints_ReplayGhostId",
                table: "GhostCheckpoints",
                column: "ReplayGhostId");

            migrationBuilder.CreateIndex(
                name: "IX_ReplayGhosts_ReplayId",
                table: "ReplayGhosts",
                column: "ReplayId");

            migrationBuilder.CreateIndex(
                name: "IX_Replays_Guid",
                table: "Replays",
                column: "Guid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GhostCheckpoints_ReplayGhosts_ReplayGhostId",
                table: "GhostCheckpoints",
                column: "ReplayGhostId",
                principalTable: "ReplayGhosts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TMFCampaignScoresRecords_Replays_ReplayId",
                table: "TMFCampaignScoresRecords",
                column: "ReplayId",
                principalTable: "Replays",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GhostCheckpoints_ReplayGhosts_ReplayGhostId",
                table: "GhostCheckpoints");

            migrationBuilder.DropForeignKey(
                name: "FK_TMFCampaignScoresRecords_Replays_ReplayId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.DropTable(
                name: "ReplayGhosts");

            migrationBuilder.DropTable(
                name: "Replays");

            migrationBuilder.DropIndex(
                name: "IX_TMFCampaignScoresRecords_ReplayId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.DropIndex(
                name: "IX_GhostCheckpoints_ReplayGhostId",
                table: "GhostCheckpoints");

            migrationBuilder.DropColumn(
                name: "ReplayId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.DropColumn(
                name: "ReplayGhostId",
                table: "GhostCheckpoints");
        }
    }
}
