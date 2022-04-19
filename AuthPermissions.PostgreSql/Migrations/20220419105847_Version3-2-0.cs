using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPermissions.PostgreSql.Migrations
{
    public partial class Version320 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ConnectionName",
                schema: "authp",
                table: "Tenants",
                newName: "DatabaseInfoName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DatabaseInfoName",
                schema: "authp",
                table: "Tenants",
                newName: "ConnectionName");
        }
    }
}
