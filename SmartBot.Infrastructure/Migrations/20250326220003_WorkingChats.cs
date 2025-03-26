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
            migrationBuilder.RenameColumn(
                name: "IsExaminer",
                table: "Users",
                newName: "Role");
            
            migrationBuilder.Sql(
                @"UPDATE Users 
                      SET Role = 3 
                      WHERE Role = 0;"
            );

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_WorkingChats_SelectedWorkingChatId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_WorkingChats_WorkingChatId",
                table: "Users");

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
        }
    }
}
