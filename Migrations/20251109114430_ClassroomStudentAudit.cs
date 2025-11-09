using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorLinkBe.Migrations
{
    /// <inheritdoc />
    public partial class ClassroomStudentAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JoinedAt",
                table: "ClassroomStudents",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ClassroomStudents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ClassroomStudents");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ClassroomStudents",
                newName: "JoinedAt");
        }
    }
}
