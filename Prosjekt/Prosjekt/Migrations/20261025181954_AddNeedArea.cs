using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prosjekt.Migrations
{
    /// <inheritdoc />
    public partial class AddNeedArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AreaGeoJson",
                table: "Needs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<double>(
                name: "AreaSquareMeters",
                table: "Needs",
                type: "double",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PerimeterMeters",
                table: "Needs",
                type: "double",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaGeoJson",
                table: "Needs");

            migrationBuilder.DropColumn(
                name: "AreaSquareMeters",
                table: "Needs");

            migrationBuilder.DropColumn(
                name: "PerimeterMeters",
                table: "Needs");
        }
    }
}
