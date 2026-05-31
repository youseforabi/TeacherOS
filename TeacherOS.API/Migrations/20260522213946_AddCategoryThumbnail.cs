using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryThumbnail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Courses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "Courses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CategoryId",
                table: "Courses",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Categories_CategoryId",
                table: "Courses",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Categories_CategoryId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CategoryId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "Courses");
        }
    }
}
