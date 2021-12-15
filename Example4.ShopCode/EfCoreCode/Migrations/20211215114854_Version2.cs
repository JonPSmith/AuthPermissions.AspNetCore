using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Example4.ShopCode.EfCoreCode.Migrations
{
    public partial class Version2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "retail",
                table: "ShopStocks",
                type: "varchar(250)",
                unicode: false,
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.UpdateToVersion2DataKeyFormat("retail.ShopStocks");

            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "retail",
                table: "ShopSales",
                type: "varchar(250)",
                unicode: false,
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.UpdateToVersion2DataKeyFormat("retail.ShopSales");

            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "retail",
                table: "RetailOutlets",
                type: "varchar(250)",
                unicode: false,
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.UpdateToVersion2DataKeyFormat("retail.RetailOutlets");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "retail",
                table: "ShopStocks",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(250)",
                oldUnicode: false,
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "retail",
                table: "ShopSales",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(250)",
                oldUnicode: false,
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "retail",
                table: "RetailOutlets",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(250)",
                oldUnicode: false,
                oldMaxLength: 250,
                oldNullable: true);
        }
    }
}
