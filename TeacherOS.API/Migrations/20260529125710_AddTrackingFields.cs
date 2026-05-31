using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "LessonProgresses",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "LessonProgresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastViewedAt",
                table: "LessonProgresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "LessonProgresses");

            migrationBuilder.DropColumn(
                name: "LastViewedAt",
                table: "LessonProgresses");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedAt",
                table: "LessonProgresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
