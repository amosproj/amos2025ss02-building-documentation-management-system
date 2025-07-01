using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Build.ING.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCategoryId_UseCategoryString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTagRelations_DocumentTags_TagId",
                table: "DocumentTagRelations");

            migrationBuilder.DropTable(
                name: "DocumentTags");

            migrationBuilder.DropIndex(
                name: "IX_DocumentTagRelations_TagId",
                table: "DocumentTagRelations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTags",
                columns: table => new
                {
                    TagId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTags", x => x.TagId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTagRelations_TagId",
                table: "DocumentTagRelations",
                column: "TagId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTagRelations_DocumentTags_TagId",
                table: "DocumentTagRelations",
                column: "TagId",
                principalTable: "DocumentTags",
                principalColumn: "TagId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
