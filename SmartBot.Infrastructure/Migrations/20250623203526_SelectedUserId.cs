using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SelectedUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SelectedUserId",
                table: "Users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SelectedUserId",
                table: "Users",
                column: "SelectedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_SelectedUserId",
                table: "Users",
                column: "SelectedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_SelectedUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SelectedUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SelectedUserId",
                table: "Users");
        }
    }
}
