using Microsoft.EntityFrameworkCore.Migrations;

namespace AuthPermissions.DataLayer.Migrations
{
    public partial class UserWithEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "authp",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "authp",
                table: "Users",
                column: "Email",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                schema: "authp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "authp",
                table: "Users");
        }
    }
}
