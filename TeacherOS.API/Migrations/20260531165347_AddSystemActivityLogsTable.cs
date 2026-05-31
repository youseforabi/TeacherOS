using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherOS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemActivityLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    ActorName = table.Column<string>(type: "text", nullable: false),
                    ActorRole = table.Column<string>(type: "text", nullable: false),
                    TargetName = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemActivityLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemActivityLogs");
        }
    }
}
