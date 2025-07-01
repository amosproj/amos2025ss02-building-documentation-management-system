using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Build.ING.Migrations
{
    /// <inheritdoc />
    public partial class FixCategoryProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*migrationBuilder.AlterColumn<JsonDocument>(
                name: "KeyInformation",
                table: "Documents",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");*/
            migrationBuilder.AddColumn<JsonDocument>(
                name: "KeyInformation",
                table: "Documents",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Documents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Documents");

            migrationBuilder.AlterColumn<string>(
                name: "KeyInformation",
                table: "Documents",
                type: "jsonb",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(JsonDocument),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
