using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignOrderSectionStartId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "TMFCampaigns",
                type: "varchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "StartId",
                table: "TMFCampaigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Maps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TMFCampaignId",
                table: "Maps",
                type: "varchar(32)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_TMFCampaignId",
                table: "Maps",
                column: "TMFCampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_Maps_TMFCampaigns_TMFCampaignId",
                table: "Maps",
                column: "TMFCampaignId",
                principalTable: "TMFCampaigns",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maps_TMFCampaigns_TMFCampaignId",
                table: "Maps");

            migrationBuilder.DropIndex(
                name: "IX_Maps_TMFCampaignId",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "TMFCampaigns");

            migrationBuilder.DropColumn(
                name: "StartId",
                table: "TMFCampaigns");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "TMFCampaignId",
                table: "Maps");
        }
    }
}
