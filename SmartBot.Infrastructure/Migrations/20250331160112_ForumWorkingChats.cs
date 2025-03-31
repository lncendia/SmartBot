using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ForumWorkingChats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MessageThreadId",
                table: "WorkingChats",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageThreadId",
                table: "WorkingChats");
        }
    }
}
