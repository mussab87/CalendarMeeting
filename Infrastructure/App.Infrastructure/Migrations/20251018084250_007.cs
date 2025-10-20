using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Infrastructure.Migrations
{
    public partial class _007 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeetingFinishNotes_Users_UserId",
                table: "MeetingFinishNotes");

            migrationBuilder.DropTable(
                name: "MeetingNotes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeetingFinishNotes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "MeetingFinishNotes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "MeetingFinishNoteId",
                table: "MeetingAttachments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingAttachments_MeetingFinishNoteId",
                table: "MeetingAttachments",
                column: "MeetingFinishNoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingAttachments_MeetingFinishNotes_MeetingFinishNoteId",
                table: "MeetingAttachments",
                column: "MeetingFinishNoteId",
                principalTable: "MeetingFinishNotes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingFinishNotes_Users_UserId",
                table: "MeetingFinishNotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeetingAttachments_MeetingFinishNotes_MeetingFinishNoteId",
                table: "MeetingAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_MeetingFinishNotes_Users_UserId",
                table: "MeetingFinishNotes");

            migrationBuilder.DropIndex(
                name: "IX_MeetingAttachments_MeetingFinishNoteId",
                table: "MeetingAttachments");

            migrationBuilder.DropColumn(
                name: "MeetingFinishNoteId",
                table: "MeetingAttachments");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MeetingFinishNotes",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "MeetingFinishNotes",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.CreateTable(
                name: "MeetingNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    CreatedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedById = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoteContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingNotes_Meetings_MeetingId",
                        column: x => x.MeetingId,
                        principalTable: "Meetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MeetingNotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_MeetingId",
                table: "MeetingNotes",
                column: "MeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingNotes_UserId",
                table: "MeetingNotes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingFinishNotes_Users_UserId",
                table: "MeetingFinishNotes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
