// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.SetupCode;

namespace Example7.MvcWebApp.ShardingOnly.PermissionsCode
{
    public static class Example7AppAuthSetupData
    {
        public static readonly List<BulkLoadRolesDto> RolesDefinition = new()
        {
            new("SuperAdmin", "Super admin - only use for setup", "AccessAll"),
            new("App Admin", "Overall app Admin",
                "UserRead, UserSync, UserChange, UserRemove, " +
                "UserRolesChange, RoleRead, RoleChange, PermissionRead, IncludeFilteredPermissions, " +
                "TenantList, TenantCreate, TenantUpdate, UserChangeTenant, TenantAccessData, " +
                "ListDbsWithTenants, MoveTenantDatabase, ListDatabaseInfos, AddDatabaseInfo, UpdateDatabaseInfo, RemoveDatabaseInfo"),
            new("App Support", "overall support - limited admin items",
                "UserRead, UserRolesChange, RoleRead, TenantList, TenantAccessData"),
            new("Invoice Reader", "Can read invoices", "InvoiceRead"),
            new("Invoice Creator", "Can access invoices", "InvoiceCreate"),
            new("Tenant User", "Can access invoices", "InvoiceRead, InvoiceCreate"),
            //roles linked to tenant
            new("Tenant Admin", "Tenant-level admin",
                "UserRead, UserRolesChange, RoleRead, InviteUsers", RoleTypes.TenantAdminAdd),
            new("Enterprise", "Enterprise features", "InvoiceSum", RoleTypes.TenantAutoAdd)
        };

        public static readonly List<BulkLoadUserWithRolesTenant> UsersRolesDefinition = new()
        {
            new ("Super@g1.com", null, "SuperAdmin"),
            new ("AppAdmin@g1.com", null, "App Admin"),
            new("AppSupport@g1.com", null, "App Support, Tenant User"),
            new ("extraUser@g1.com", null, ""),
        };
    }
}