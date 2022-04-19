using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPermissions.DataLayer.Migrations
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

            //This will update the version 3.0.0 to the new 3.2.0 format
            //But is only works on tenants in the database defined by the DefaultConnection string
            migrationBuilder.Sql(@"UPDATE [authp].[Tenants] 
SET [DatabaseInfoName] = 'Default Database'
WHERE [DatabaseInfoName] = 'DefaultConnection'");
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
