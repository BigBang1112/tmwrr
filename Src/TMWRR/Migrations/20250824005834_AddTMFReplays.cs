using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddTMFReplays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DrivenAt",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.AddColumn<int>(
                name: "ReplayId",
                table: "TMFCampaignScoresRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TMFReplays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Data = table.Column<byte[]>(type: "longblob", maxLength: 2000000, nullable: false),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Etag = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Url = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMFReplays", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TMFCampaignScoresRecords_ReplayId",
                table: "TMFCampaignScoresRecords",
                column: "ReplayId");

            migrationBuilder.AddForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFReplays_ReplayId",
                table: "TMFCampaignScoresRecords",
                column: "ReplayId",
                principalTable: "TMFReplays",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TMFCampaignScoresRecords_TMFReplays_ReplayId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.DropTable(
                name: "TMFReplays");

            migrationBuilder.DropIndex(
                name: "IX_TMFCampaignScoresRecords_ReplayId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.DropColumn(
                name: "ReplayId",
                table: "TMFCampaignScoresRecords");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DrivenAt",
                table: "TMFCampaignScoresRecords",
                type: "datetime(6)",
                nullable: true);
        }
    }
}
