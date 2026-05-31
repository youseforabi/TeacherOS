using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Content",
                table: "Lessons",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "Lessons",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Content",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "Lessons");
        }
    }
}
