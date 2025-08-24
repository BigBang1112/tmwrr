using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddNicknameDeformatted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "TMFReplays",
                type: "mediumblob",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 2000000);

            migrationBuilder.AddColumn<string>(
                name: "NicknameDeformatted",
                table: "TMFLogins",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Thumbnail",
                table: "Maps",
                type: "mediumblob",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "longblob",
                oldMaxLength: 512000,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NicknameDeformatted",
                table: "TMFLogins");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Data",
                table: "TMFReplays",
                type: "longblob",
                maxLength: 2000000,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Thumbnail",
                table: "Maps",
                type: "longblob",
                maxLength: 512000,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "mediumblob",
                oldNullable: true);
        }
    }
}
