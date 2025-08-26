using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotGuids : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Guid",
                table: "TMFLadderScoresSnapshots",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "Guid",
                table: "TMFCampaignScoresSnapshots",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_TMFLadderScoresSnapshots_Guid",
                table: "TMFLadderScoresSnapshots",
                column: "Guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresSnapshots_Guid",
                table: "TMFCampaignScoresSnapshots",
                column: "Guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TMFLadderScoresSnapshots_Guid",
                table: "TMFLadderScoresSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_TMFCampaignScoresSnapshots_Guid",
                table: "TMFCampaignScoresSnapshots");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "TMFLadderScoresSnapshots");

            migrationBuilder.DropColumn(
                name: "Guid",
                table: "TMFCampaignScoresSnapshots");
        }
    }
}
