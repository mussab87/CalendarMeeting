using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    public partial class _003 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "Meetings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendedAt",
                table: "MeetingParticipants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAttended",
                table: "MeetingParticipants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MeetingFinishNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NoteFinishContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingFinishNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingFinishNotes_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingFinishNotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingPriorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PriorityColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingPriorities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Meetings_PriorityId",
                table: "Meetings",
                column: "PriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingFinishNotes_MeetingId",
                table: "MeetingFinishNotes",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingFinishNotes_UserId",
                table: "MeetingFinishNotes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Meetings_MeetingPriorities_PriorityId",
                table: "Meetings",
                column: "PriorityId",
                principalTable: "MeetingPriorities",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meetings_MeetingPriorities_PriorityId",
                table: "Meetings");

            migrationBuilder.DropTable(
                name: "MeetingFinishNotes");

            migrationBuilder.DropTable(
                name: "MeetingPriorities");

            migrationBuilder.DropIndex(
                name: "IX_Meetings_PriorityId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "Meetings");

            migrationBuilder.DropColumn(
                name: "AttendedAt",
                table: "MeetingParticipants");

            migrationBuilder.DropColumn(
                name: "IsAttended",
                table: "MeetingParticipants");
        }
    }
}
