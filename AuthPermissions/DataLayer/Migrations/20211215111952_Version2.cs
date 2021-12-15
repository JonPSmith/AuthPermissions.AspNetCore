using System;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthPermissions.DataLayer.Migrations
{
    public partial class Version2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuthUsers_Email",
                schema: "authp",
                table: "AuthUsers");

            migrationBuilder.AlterColumn<string>(
                name: "TenantFullName",
                schema: "authp",
                table: "Tenants",
                type: "nvarchar(400)",
                maxLength: 400,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ParentDataKey",
                schema: "authp",
                table: "Tenants",
                type: "varchar(250)",
                unicode: false,
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "RoleType",
                schema: "authp",
                table: "RoleToPermissions",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "authp",
                table: "AuthUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateTable(
                name: "RoleToPermissionsTenant",
                schema: "authp",
                columns: table => new
                {
                    TenantRolesRoleName = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TenantsTenantId = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyToken = table.Column<byte[]>(type: "ROWVERSION", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleToPermissionsTenant", x => new { x.TenantRolesRoleName, x.TenantsTenantId });
                    table.ForeignKey(
                        name: "FK_RoleToPermissionsTenant_RoleToPermissions_TenantRolesRoleName",
                        column: x => x.TenantRolesRoleName,
                        principalSchema: "authp",
                        principalTable: "RoleToPermissions",
                        principalColumn: "RoleName",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleToPermissionsTenant_Tenants_TenantsTenantId",
                        column: x => x.TenantsTenantId,
                        principalSchema: "authp",
                        principalTable: "Tenants",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleToPermissions_RoleType",
                schema: "authp",
                table: "RoleToPermissions",
                column: "RoleType");

            migrationBuilder.CreateIndex(
                name: "IX_AuthUsers_Email",
                schema: "authp",
                table: "AuthUsers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuthUsers_UserName",
                schema: "authp",
                table: "AuthUsers",
                column: "UserName",
                unique: true,
                filter: "[UserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RoleToPermissionsTenant_TenantsTenantId",
                schema: "authp",
                table: "RoleToPermissionsTenant",
                column: "TenantsTenantId");

            migrationBuilder.UpdateToVersion2DataKeyFormat("authp.Tenants", "ParentDataKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleToPermissionsTenant",
                schema: "authp");

            migrationBuilder.DropIndex(
                name: "IX_RoleToPermissions_RoleType",
                schema: "authp",
                table: "RoleToPermissions");

            migrationBuilder.DropIndex(
                name: "IX_AuthUsers_Email",
                schema: "authp",
                table: "AuthUsers");

            migrationBuilder.DropIndex(
                name: "IX_AuthUsers_UserName",
                schema: "authp",
                table: "AuthUsers");

            migrationBuilder.DropColumn(
                name: "RoleType",
                schema: "authp",
                table: "RoleToPermissions");

            migrationBuilder.AlterColumn<string>(
                name: "TenantFullName",
                schema: "authp",
                table: "Tenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(400)",
                oldMaxLength: 400);

            migrationBuilder.AlterColumn<string>(
                name: "ParentDataKey",
                schema: "authp",
                table: "Tenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(250)",
                oldUnicode: false,
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                schema: "authp",
                table: "AuthUsers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthUsers_Email",
                schema: "authp",
                table: "AuthUsers",
                column: "Email",
                unique: true);
        }
    }
}
