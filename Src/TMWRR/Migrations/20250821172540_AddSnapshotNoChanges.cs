using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotNoChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoChanges",
                table: "TMFCampaignScoresSnapshots",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoChanges",
                table: "TMFCampaignScoresSnapshots");
        }
    }
}
