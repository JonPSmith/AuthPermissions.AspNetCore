using Microsoft.EntityFrameworkCore.Migrations;

namespace Example4.ShopCode.EfCoreCode.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "retail");

            migrationBuilder.CreateTable(
                name: "RetailOutlets",
                schema: "retail",
                columns: table => new
                {
                    RetailOutletId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AuthPTenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailOutlets", x => x.RetailOutletId);
                });

            migrationBuilder.CreateTable(
                name: "ShopStocks",
                schema: "retail",
                columns: table => new
                {
                    ShopStockId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetailPrice = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    NumInStock = table.Column<int>(type: "int", nullable: false),
                    DataKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TenantItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopStocks", x => x.ShopStockId);
                    table.ForeignKey(
                        name: "FK_ShopStocks_RetailOutlets_TenantItemId",
                        column: x => x.TenantItemId,
                        principalSchema: "retail",
                        principalTable: "RetailOutlets",
                        principalColumn: "RetailOutletId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShopSales",
                schema: "retail",
                columns: table => new
                {
                    ShopSaleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumSoldReturned = table.Column<int>(type: "int", nullable: false),
                    ReturnReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ShopStockId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopSales", x => x.ShopSaleId);
                    table.ForeignKey(
                        name: "FK_ShopSales_ShopStocks_ShopStockId",
                        column: x => x.ShopStockId,
                        principalSchema: "retail",
                        principalTable: "ShopStocks",
                        principalColumn: "ShopStockId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetailOutlets_DataKey",
                schema: "retail",
                table: "RetailOutlets",
                column: "DataKey");

            migrationBuilder.CreateIndex(
                name: "IX_RetailOutlets_FullName",
                schema: "retail",
                table: "RetailOutlets",
                column: "FullName",
                unique: true,
                filter: "[FullName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShopSales_DataKey",
                schema: "retail",
                table: "ShopSales",
                column: "DataKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShopSales_ShopStockId",
                schema: "retail",
                table: "ShopSales",
                column: "ShopStockId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopStocks_DataKey",
                schema: "retail",
                table: "ShopStocks",
                column: "DataKey");

            migrationBuilder.CreateIndex(
                name: "IX_ShopStocks_TenantItemId",
                schema: "retail",
                table: "ShopStocks",
                column: "TenantItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopSales",
                schema: "retail");

            migrationBuilder.DropTable(
                name: "ShopStocks",
                schema: "retail");

            migrationBuilder.DropTable(
                name: "RetailOutlets",
                schema: "retail");
        }
    }
}
