using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BitAndBeam.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedDocumentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql("DROP TABLE IF EXISTS \"DocumentTags\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"DocumentTagRelations\" CASCADE;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"DocumentPermissions\" CASCADE;");


            migrationBuilder.AlterColumn<JsonDocument>(
                name: "KeyInformation",
                table: "Documents",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
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

            migrationBuilder.AddColumn<string>(
                name: "GroupId",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DocumentPermissions",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    GrantedBy = table.Column<int>(type: "integer", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PermissionType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPermissions", x => new { x.DocumentId, x.UserId });
                    table.ForeignKey(
                        name: "FK_DocumentPermissions_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentPermissions_Users_GrantedBy",
                        column: x => x.GrantedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DocumentPermissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

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

            migrationBuilder.CreateTable(
                name: "DocumentTagRelations",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    TagId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTagRelations", x => new { x.DocumentId, x.TagId });
                    table.ForeignKey(
                        name: "FK_DocumentTagRelations_DocumentTags_TagId",
                        column: x => x.TagId,
                        principalTable: "DocumentTags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentTagRelations_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPermissions_GrantedBy",
                table: "DocumentPermissions",
                column: "GrantedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPermissions_UserId",
                table: "DocumentPermissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTagRelations_TagId",
                table: "DocumentTagRelations",
                column: "TagId");
        }
    }
}
