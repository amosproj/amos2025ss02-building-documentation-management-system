using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Build.ING.Migrations
{
    /// <inheritdoc />
    public partial class DropCategoryIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Optionally restore the column if rolling back
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "Documents",
                type: "integer",
                nullable: true);
        }
    }
}
