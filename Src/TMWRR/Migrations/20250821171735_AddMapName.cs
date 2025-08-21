using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddMapName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeformattedName",
                table: "Maps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Maps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeformattedName",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Maps");
        }
    }
}
