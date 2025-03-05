using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportsOverdue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EveningReport",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "MorningReport",
                table: "Reports",
                newName: "EveningReport_Data");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EveningReport_Overdue",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "MorningReport_Data",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "MorningReport_Overdue",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "EveningReport_Data",
                table: "Reports",
                newName: "MorningReport");

            migrationBuilder.AddColumn<string>(
                name: "EveningReport",
                table: "Reports",
                type: "TEXT",
                maxLength: 5000,
                nullable: true);
        }
    }
}
