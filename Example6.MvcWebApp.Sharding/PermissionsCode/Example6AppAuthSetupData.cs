// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;

namespace Example6.MvcWebApp.Sharding.PermissionsCode
{
    public static class Example6AppAuthSetupData
    {
        public static readonly List<BulkLoadRolesDto> RolesDefinition = new()
        {
            new("SuperAdmin", "Super admin - only use for setup", "AccessAll"),
            new("App Admin", "Overall app Admin",
                "UserRead, UserSync, UserChange, UserRemove, " +
                "UserRolesChange, RoleRead, RoleChange, PermissionRead, IncludeFilteredPermissions, " +
                "TenantList, TenantCreate, TenantUpdate, UserChangeTenant, TenantAccessData, " +
                "ListDbsWithTenants, MoveTenantDatabase, ListDatabaseInfos, AddDatabaseInfo, UpdateDatabaseInfo, RemoveDatabaseInfo, " +
                "AppStatusList, AppStatusAllDown, AppStatusTenantDown, AppStatusRemove"),
            new("App Support", "overall support - limited admin items",
                "UserRead, UserRolesChange, RoleRead, TenantList, TenantAccessData"),
            new("Tenant User", "Can access invoices", "InvoiceRead, InvoiceCreate"),
            new("Tenant Admin", "Tenant-level admin",
                "UserRead, UserRolesChange, RoleRead"),
        };

        public static readonly List<BulkLoadTenantDto> TenantDefinition = new()
        {
            new("4U Inc."),
            new("Pets Ltd."),
            new("Big Rocks Inc.")
        };

        public static readonly List<BulkLoadUserWithRolesTenant> UsersRolesDefinition = new()
        {
            new ("Super@g1.com", null, "SuperAdmin"),
            new ("AppAdmin@g1.com", null, "App Admin"),
            new("AppSupport@g1.com", null, "App Support, Tenant User"),
            new ("extraUser@g1.com", null, "Tenant User"),
            //Company admins.
            new ("admin@4uInc.com", null,
                "Tenant User, Tenant Admin", tenantNameForDataKey: "4U Inc."),
            new("admin1@Pets.com", null,
                "Tenant User, Tenant Admin", tenantNameForDataKey: "Pets Ltd."),
            //Company users.
            new ("user1@4uInc.com", null,
                "Tenant User", tenantNameForDataKey: "4U Inc."),
            new ("user2@4uInc.com", null,
                "Tenant User", tenantNameForDataKey: "4U Inc."),
            new ("user1@Pets.com", null, "Tenant User", tenantNameForDataKey: "Pets Ltd."),
            new ("user1@BigR.com", null, "Tenant User", tenantNameForDataKey: "Big Rocks Inc."),
        };
    }
}