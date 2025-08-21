using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMWRR.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreMapFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "TMFCampaigns",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AuthorId",
                table: "Maps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorScore",
                table: "Maps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AuthorTime",
                table: "Maps",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentId",
                table: "Maps",
                type: "varchar(16)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Maps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ModeId",
                table: "Maps",
                type: "varchar(16)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "NbLaps",
                table: "Maps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<byte[]>(
                name: "Thumbnail",
                table: "Maps",
                type: "longblob",
                maxLength: 512000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(12)", maxLength: 12, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Modes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Guid = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LoginTMFId = table.Column<string>(type: "varchar(32)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_TMFLogins_LoginTMFId",
                        column: x => x.LoginTMFId,
                        principalTable: "TMFLogins",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Environments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GameId = table.Column<string>(type: "varchar(12)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Environments_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_AuthorId",
                table: "Maps",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_EnvironmentId",
                table: "Maps",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Maps_ModeId",
                table: "Maps",
                column: "ModeId");

            migrationBuilder.CreateIndex(
                name: "IX_Environments_GameId",
                table: "Environments",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Guid",
                table: "Users",
                column: "Guid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LoginTMFId",
                table: "Users",
                column: "LoginTMFId");

            migrationBuilder.AddForeignKey(
                name: "FK_Maps_Environments_EnvironmentId",
                table: "Maps",
                column: "EnvironmentId",
                principalTable: "Environments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Maps_Modes_ModeId",
                table: "Maps",
                column: "ModeId",
                principalTable: "Modes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Maps_Users_AuthorId",
                table: "Maps",
                column: "AuthorId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maps_Environments_EnvironmentId",
                table: "Maps");

            migrationBuilder.DropForeignKey(
                name: "FK_Maps_Modes_ModeId",
                table: "Maps");

            migrationBuilder.DropForeignKey(
                name: "FK_Maps_Users_AuthorId",
                table: "Maps");

            migrationBuilder.DropTable(
                name: "Environments");

            migrationBuilder.DropTable(
                name: "Modes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Maps_AuthorId",
                table: "Maps");

            migrationBuilder.DropIndex(
                name: "IX_Maps_EnvironmentId",
                table: "Maps");

            migrationBuilder.DropIndex(
                name: "IX_Maps_ModeId",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "TMFCampaigns");

            migrationBuilder.DropColumn(
                name: "AuthorId",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "AuthorScore",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "AuthorTime",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "ModeId",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "NbLaps",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "Maps");
        }
    }
}
