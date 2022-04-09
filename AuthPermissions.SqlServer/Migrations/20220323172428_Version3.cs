using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPermissions.DataLayer.Migrations
{
    public partial class Version3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConnectionName",
                schema: "authp",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasOwnDb",
                schema: "authp",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                schema: "authp",
                table: "AuthUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConnectionName",
                schema: "authp",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "HasOwnDb",
                schema: "authp",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsDisabled",
                schema: "authp",
                table: "AuthUsers");
        }
    }
}
