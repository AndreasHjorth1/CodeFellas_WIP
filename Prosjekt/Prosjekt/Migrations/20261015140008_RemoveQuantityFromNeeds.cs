using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prosjekt.Migrations
{
    /// <inheritdoc />
    public partial class RemoveQuantityFromNeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Needs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Needs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
