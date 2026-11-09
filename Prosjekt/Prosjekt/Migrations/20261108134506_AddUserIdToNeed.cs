using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prosjekt.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToNeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Needs",
                type: "varchar(255)",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Needs_UserId",
                table: "Needs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Needs_AspNetUsers_UserId",
                table: "Needs",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Needs_AspNetUsers_UserId",
                table: "Needs");

            migrationBuilder.DropIndex(
                name: "IX_Needs_UserId",
                table: "Needs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Needs");
        }
    }
}
