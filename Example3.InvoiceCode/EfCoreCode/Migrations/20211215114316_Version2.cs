using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Example3.InvoiceCode.EfCoreCode.Migrations
{
    public partial class Version2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "invoice",
                table: "LineItems",
                type: "varchar(12)",
                unicode: false,
                maxLength: 12,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.UpdateToVersion2DataKeyFormat("invoice.LineItems");

            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "invoice",
                table: "Invoices",
                type: "varchar(12)",
                unicode: false,
                maxLength: 12,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.UpdateToVersion2DataKeyFormat("invoice.Invoices");

            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "invoice",
                table: "Companies",
                type: "varchar(12)",
                unicode: false,
                maxLength: 12,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.UpdateToVersion2DataKeyFormat("invoice.Companies");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "invoice",
                table: "LineItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldUnicode: false,
                oldMaxLength: 12,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "invoice",
                table: "Invoices",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldUnicode: false,
                oldMaxLength: 12,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DataKey",
                schema: "invoice",
                table: "Companies",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldUnicode: false,
                oldMaxLength: 12,
                oldNullable: true);
        }
    }
}
