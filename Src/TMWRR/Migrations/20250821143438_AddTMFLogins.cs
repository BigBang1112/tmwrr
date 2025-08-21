using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddTMFLogins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFLogin_PlayerId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TMFLogin",
                table: "TMFLogin");

            migrationBuilder.RenameTable(
                name: "TMFLogin",
                newName: "TMFLogins");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DrivenAt",
                table: "TMFCampaignScoresRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TMFLogins",
                table: "TMFLogins",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFLogins_PlayerId",
                table: "TMFCampaignScoresRecords",
                column: "PlayerId",
                principalTable: "TMFLogins",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFLogins_PlayerId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TMFLogins",
                table: "TMFLogins");

            migrationBuilder.DropColumn(
                name: "DrivenAt",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.RenameTable(
                name: "TMFLogins",
                newName: "TMFLogin");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TMFLogin",
                table: "TMFLogin",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFLogin_PlayerId",
                table: "TMFCampaignScoresRecords",
                column: "PlayerId",
                principalTable: "TMFLogin",
                principalColumn: "Id");
        }
    }
}
