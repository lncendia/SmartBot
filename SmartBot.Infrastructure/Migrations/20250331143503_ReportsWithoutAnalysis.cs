using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReportsWithoutAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentReport",
                table: "Users",
                type: "TEXT",
                maxLength: 5000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentReport",
                table: "Users");
        }
    }
}
