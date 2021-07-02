using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AuthPermissions.DataLayer.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "authp");

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                schema: "authp",
                columns: table => new
                {
                    TokenValue = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    JwtId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsInvalid = table.Column<bool>(type: "bit", nullable: false),
                    AddedDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConcurrencyToken = table.Column<byte[]>(type: "ROWVERSION", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.TokenValue);
                });

            migrationBuilder.CreateTable(
                name: "RoleToPermissions",
                schema: "authp",
                columns: table => new
                {
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackedPermissionsInRole = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyToken = table.Column<byte[]>(type: "ROWVERSION", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleToPermissions", x => x.RoleName);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "authp",
                columns: table => new
                {
                    TenantId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentDataKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TenantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsHierarchical = table.Column<bool>(type: "bit", nullable: false),
                    ParentTenantId = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyToken = table.Column<byte[]>(type: "ROWVERSION", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.TenantId);
                    table.ForeignKey(
                        name: "FK_Tenants_Tenants_ParentTenantId",
                        column: x => x.ParentTenantId,
                        principalSchema: "authp",
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthUsers",
                schema: "authp",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyToken = table.Column<byte[]>(type: "ROWVERSION", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthUsers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_AuthUsers_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "authp",
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserToRoles",
                schema: "authp",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConcurrencyToken = table.Column<byte[]>(type: "ROWVERSION", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserToRoles", x => new { x.UserId, x.RoleName });
                    table.ForeignKey(
                        name: "FK_UserToRoles_AuthUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "authp",
                        principalTable: "AuthUsers",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserToRoles_RoleToPermissions_RoleName",
                        column: x => x.RoleName,
                        principalSchema: "authp",
                        principalTable: "RoleToPermissions",
                        principalColumn: "RoleName",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthUsers_Email",
                schema: "authp",
                table: "AuthUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthUsers_TenantId",
                schema: "authp",
                table: "AuthUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AddedDateUtc",
                schema: "authp",
                table: "RefreshTokens",
                column: "AddedDateUtc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ParentDataKey",
                schema: "authp",
                table: "Tenants",
                column: "ParentDataKey");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_ParentTenantId",
                schema: "authp",
                table: "Tenants",
                column: "ParentTenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TenantName",
                schema: "authp",
                table: "Tenants",
                column: "TenantName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserToRoles_RoleName",
                schema: "authp",
                table: "UserToRoles",
                column: "RoleName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "authp");

            migrationBuilder.DropTable(
                name: "UserToRoles",
                schema: "authp");

            migrationBuilder.DropTable(
                name: "AuthUsers",
                schema: "authp");

            migrationBuilder.DropTable(
                name: "RoleToPermissions",
                schema: "authp");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "authp");
        }
    }
}
