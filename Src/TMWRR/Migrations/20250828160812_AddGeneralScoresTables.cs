using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralScoresTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TMFGeneralScoresSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PlayerCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    NoChanges = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMFGeneralScoresSnapshots", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TMFGeneralScoresPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SnapshotId = table.Column<int>(type: "int", nullable: false),
                    PlayerId = table.Column<string>(type: "varchar(32)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TMFGeneralScoresPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TMFGeneralScoresPlayers_TMFGeneralScoresSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "TMFGeneralScoresSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TMFGeneralScoresPlayers_TMFLogins_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "TMFLogins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TMFGeneralScoresPlayers_PlayerId",
                table: "TMFGeneralScoresPlayers",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_TMFGeneralScoresPlayers_SnapshotId",
                table: "TMFGeneralScoresPlayers",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_TMFGeneralScoresSnapshots_CreatedAt",
                table: "TMFGeneralScoresSnapshots",
                column: "CreatedAt",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TMFGeneralScoresSnapshots_Guid",
                table: "TMFGeneralScoresSnapshots",
                column: "Guid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TMFGeneralScoresPlayers");

            migrationBuilder.DropTable(
                name: "TMFGeneralScoresSnapshots");
        }
    }
}
