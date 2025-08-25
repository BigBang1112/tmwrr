using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class RenameReplayToGhost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFLogins_PlayerId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFReplays_ReplayId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.RenameTable(
                name: "TMFReplays",
                newName: "Ghosts");

            migrationBuilder.RenameColumn(
                name: "ReplayId",
                table: "TMFCampaignScoresRecords",
                newName: "GhostId");

            migrationBuilder.RenameIndex(
                name: "IX_TMFCampaignScoresRecords_ReplayId",
                table: "TMFCampaignScoresRecords",
                newName: "IX_TMFCampaignScoresRecords_GhostId");

            migrationBuilder.UpdateData(
                table: "TMFCampaignScoresRecords",
                keyColumn: "PlayerId",
                keyValue: null,
                column: "PlayerId",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "PlayerId",
                table: "TMFCampaignScoresRecords",
                type: "varchar(32)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Ghosts_Guid",
                table: "Ghosts",
                column: "Guid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TMFCampaignScoresRecords_Ghosts_GhostId",
                table: "TMFCampaignScoresRecords",
                column: "GhostId",
                principalTable: "Ghosts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFLogins_PlayerId",
                table: "TMFCampaignScoresRecords",
                column: "PlayerId",
                principalTable: "TMFLogins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TMFCampaignScoresRecords_Ghosts_GhostId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFLogins_PlayerId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.RenameTable(
                name: "Ghosts",
                newName: "TMFReplays");

            migrationBuilder.RenameColumn(
                name: "GhostId",
                table: "TMFCampaignScoresRecords",
                newName: "ReplayId");

            migrationBuilder.RenameIndex(
                name: "IX_TMFCampaignScoresRecords_GhostId",
                table: "TMFCampaignScoresRecords",
                newName: "IX_TMFCampaignScoresRecords_ReplayId");

            migrationBuilder.AlterColumn<string>(
                name: "PlayerId",
                table: "TMFCampaignScoresRecords",
                type: "varchar(32)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(32)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TMFReplays_Guid",
                table: "TMFReplays",
                column: "Guid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFLogins_PlayerId",
                table: "TMFCampaignScoresRecords",
                column: "PlayerId",
                principalTable: "TMFLogins",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFReplays_ReplayId",
                table: "TMFCampaignScoresRecords",
                column: "ReplayId",
                principalTable: "TMFReplays",
                principalColumn: "Id");
        }
    }
}
