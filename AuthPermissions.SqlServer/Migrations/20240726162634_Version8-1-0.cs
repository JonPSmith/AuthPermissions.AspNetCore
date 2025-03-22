using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPermissions.DataLayer.Migrations
{
    /// <inheritdoc />
    public partial class Version810 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShardingEntryBackup",
                schema: "authp",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectionName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DatabaseType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyToken = table.Column<byte[]>(type: "ROWVERSION", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShardingEntryBackup", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShardingEntryBackup_Name",
                schema: "authp",
                table: "ShardingEntryBackup",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShardingEntryBackup",
                schema: "authp");
        }
    }
}
