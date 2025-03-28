using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WorkingChats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Reports_ReviewingReportId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ReviewingReportId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReviewingReportId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "IsExaminer",
                table: "Users",
                newName: "Role");

            migrationBuilder.AddColumn<long>(
                name: "SelectedWorkingChatId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WorkingChatId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AnswersFor",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    ToUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    ReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EveningReport = table.Column<bool>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnswersFor", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_AnswersFor_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnswersFor_Users_ToUserId",
                        column: x => x.ToUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AnswersFor_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewingReports",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    ReportId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EveningReport = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewingReports", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_ReviewingReports_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewingReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkingChats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkingChats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SelectedWorkingChatId",
                table: "Users",
                column: "SelectedWorkingChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_WorkingChatId",
                table: "Users",
                column: "WorkingChatId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswersFor_ReportId",
                table: "AnswersFor",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_AnswersFor_ToUserId",
                table: "AnswersFor",
                column: "ToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewingReports_ReportId",
                table: "ReviewingReports",
                column: "ReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_WorkingChats_SelectedWorkingChatId",
                table: "Users",
                column: "SelectedWorkingChatId",
                principalTable: "WorkingChats",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_WorkingChats_WorkingChatId",
                table: "Users",
                column: "WorkingChatId",
                principalTable: "WorkingChats",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
            
            migrationBuilder.Sql(
                @"UPDATE Users 
                      SET Role = 3 
                      WHERE Role = 0;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE Users 
                      SET Role = 0 
                      WHERE Role = 3;"
            );
            
            migrationBuilder.DropForeignKey(
                name: "FK_Users_WorkingChats_SelectedWorkingChatId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_WorkingChats_WorkingChatId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AnswersFor");

            migrationBuilder.DropTable(
                name: "ReviewingReports");

            migrationBuilder.DropTable(
                name: "WorkingChats");

            migrationBuilder.DropIndex(
                name: "IX_Users_SelectedWorkingChatId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_WorkingChatId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelectedWorkingChatId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkingChatId",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "Users",
                newName: "IsExaminer");

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewingReportId",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ReviewingReportId",
                table: "Users",
                column: "ReviewingReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Reports_ReviewingReportId",
                table: "Users",
                column: "ReviewingReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
