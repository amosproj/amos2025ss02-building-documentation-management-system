using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BUILD.ING.Migrations
{
    public partial class AddFileNameToDocument : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Documents");
        }
    }
}
