using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prosjekt.Migrations
{
    /// <inheritdoc />
    public partial class NeedStatusFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Legg til den nye kolonnen først. Alle eksisterende behov starter som 0 = Ny
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Needs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Behov som var oppfylt blir 3 = Løst
            migrationBuilder.Sql("UPDATE Needs SET Status = 3 WHERE IsFulfilled = 1;");

            // Først nå kan den gamle kolonnen fjernes
            migrationBuilder.DropColumn(
                name: "IsFulfilled",
                table: "Needs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFulfilled",
                table: "Needs",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Kun Løst (3) regnes som oppfylt
            migrationBuilder.Sql("UPDATE Needs SET IsFulfilled = 1 WHERE Status = 3;");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Needs");
        }
    }
}
