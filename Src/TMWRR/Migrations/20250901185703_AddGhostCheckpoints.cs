using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddGhostCheckpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GhostCheckpoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<int>(type: "int", nullable: true),
                    StuntsScore = table.Column<int>(type: "int", nullable: true),
                    Speed = table.Column<float>(type: "float", nullable: true),
                    GhostId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhostCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GhostCheckpoints_Ghosts_GhostId",
                        column: x => x.GhostId,
                        principalTable: "Ghosts",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GhostCheckpoints_GhostId",
                table: "GhostCheckpoints",
                column: "GhostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GhostCheckpoints");
        }
    }
}
