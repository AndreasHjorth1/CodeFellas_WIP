using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prosjekt.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Resources",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_UserId",
                table: "Resources",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_AspNetUsers_UserId",
                table: "Resources",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resources_AspNetUsers_UserId",
                table: "Resources");

            migrationBuilder.DropIndex(
                name: "IX_Resources_UserId",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Resources");
        }
    }
}
