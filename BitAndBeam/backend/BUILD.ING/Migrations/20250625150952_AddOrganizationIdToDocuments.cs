using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Build.ING.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Documents");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "GroupId",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
