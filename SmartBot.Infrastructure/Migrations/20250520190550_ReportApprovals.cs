using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Reports");
            
            migrationBuilder.DropColumn(
                name: "EveningReport_Data",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "EveningReport_Overdue",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "MorningReport_Data",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "MorningReport_Overdue",
                table: "Reports");

            migrationBuilder.AddColumn<int>(
                name: "EveningReportId",
                table: "Reports",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MorningReportId",
                table: "Reports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ReportElements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Data = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: false),
                    Overdue = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Approved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovedBySystem = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportElements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_EveningReportId",
                table: "Reports",
                column: "EveningReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_MorningReportId",
                table: "Reports",
                column: "MorningReportId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_ReportElements_EveningReportId",
                table: "Reports",
                column: "EveningReportId",
                principalTable: "ReportElements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_ReportElements_MorningReportId",
                table: "Reports",
                column: "MorningReportId",
                principalTable: "ReportElements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_ReportElements_EveningReportId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_ReportElements_MorningReportId",
                table: "Reports");

            migrationBuilder.DropTable(
                name: "ReportElements");

            migrationBuilder.DropIndex(
                name: "IX_Reports_EveningReportId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_MorningReportId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "EveningReportId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "MorningReportId",
                table: "Reports");

            migrationBuilder.AddColumn<string>(
                name: "EveningReport_Data",
                table: "Reports",
                type: "TEXT",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EveningReport_Overdue",
                table: "Reports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MorningReport_Data",
                table: "Reports",
                type: "TEXT",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "MorningReport_Overdue",
                table: "Reports",
                type: "TEXT",
                nullable: true);
        }
    }
}
