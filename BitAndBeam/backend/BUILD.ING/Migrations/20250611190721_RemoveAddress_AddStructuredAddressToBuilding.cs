using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Build.ING.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAddress_AddStructuredAddressToBuilding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Buildings",
                newName: "StreetName");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Buildings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Buildings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HouseNumber",
                table: "Buildings",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Buildings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "Buildings");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Buildings");

            migrationBuilder.RenameColumn(
                name: "StreetName",
                table: "Buildings",
                newName: "Address");
        }
    }
}
