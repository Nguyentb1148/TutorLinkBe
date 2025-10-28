using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorLinkBe.Migrations
{
    /// <inheritdoc />
    public partial class addRequestAndRoleHistoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OldRole = table.Column<string>(type: "text", nullable: false),
                    NewRole = table.Column<string>(type: "text", nullable: false),
                    ChangedBy = table.Column<string>(type: "text", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleHistories_AspNetUsers_ChangedBy",
                        column: x => x.ChangedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RoleHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TutorRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TutorRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleHistories_ChangedBy",
                table: "RoleHistories",
                column: "ChangedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RoleHistories_UserId",
                table: "RoleHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TutorRequests_UserId",
                table: "TutorRequests",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleHistories");

            migrationBuilder.DropTable(
                name: "TutorRequests");
        }
    }
}
