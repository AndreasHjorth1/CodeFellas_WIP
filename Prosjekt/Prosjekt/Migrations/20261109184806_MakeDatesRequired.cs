using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prosjekt.Migrations
{
    /// <inheritdoc />
    public partial class MakeDatesRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /// Gamle rader uten dato får opprettelsestidspunktet, ellers blir de 0001-01-01
            migrationBuilder.Sql("UPDATE Needs SET NeededBy = CreatedAt WHERE NeededBy IS NULL;");
            migrationBuilder.Sql("UPDATE Resources SET AvailableFrom = CreatedAt WHERE AvailableFrom IS NULL;");
            migrationBuilder.Sql("UPDATE Resources SET AvailableTo = CreatedAt WHERE AvailableTo IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AvailableTo",
                table: "Resources",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "AvailableFrom",
                table: "Resources",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "NeededBy",
                table: "Needs",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "AvailableTo",
                table: "Resources",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "AvailableFrom",
                table: "Resources",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "NeededBy",
                table: "Needs",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");
        }
    }
}
