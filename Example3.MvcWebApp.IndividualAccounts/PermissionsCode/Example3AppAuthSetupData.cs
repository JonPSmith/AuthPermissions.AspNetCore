// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.SetupCode;

namespace Example3.MvcWebApp.IndividualAccounts.PermissionsCode
{
    public static class Example3AppAuthSetupData
    {
        public static readonly List<BulkLoadRolesDto> RolesDefinition = new List<BulkLoadRolesDto>()
        {
            new("SuperAdmin", "Super admin - only use for setup", "AccessAll"),
            new("App Admin", "Overall app Admin", 
                "UserRead, UserSync, UserChange, UserRolesChange, UserChangeTenant, " +
                "UserRemove, RoleRead, RoleChange, PermissionRead, IncludeFilteredPermissions, " +
                "TenantList, TenantCreate, TenantUpdate"),
            new("Tenant Admin", "Tenant-level admin", "InvoiceRead, EmployeeRead, EmployeeRevokeActivate"),
            new("Tenant User", "Can access invoices", "InvoiceRead, InvoiceCreate"),
        };

        public static readonly List<BulkLoadTenantDto> TenantDefinition = new List<BulkLoadTenantDto>()
        {
            new("4U Inc."),
            new("Pets Ltd."),
            new("Big Rocks Inc."),
        };

        public static readonly List<BulkLoadUserWithRolesTenant> UsersRolesDefinition = new List<BulkLoadUserWithRolesTenant>
        {
            new ("Super@g1.com", null, "SuperAdmin"),
            new ("AppAdmin@g1.com", null, "App Admin"),
            new ("extraUser@g1.com", null, "Tenant User"),
            //Company admins.
            new ("admin@4uInc.com", null,
                "Tenant Admin,Tenant User", tenantNameForDataKey: "4U Inc."),
            //Company users
            new ("user1@4uInc.com", null,
                "Tenant User", tenantNameForDataKey: "4U Inc."),
            new ("user2@4uInc.com", null,
                "Tenant User", tenantNameForDataKey: "4U Inc."),
            new ("user1@Pets.com", null,
                "Tenant User", tenantNameForDataKey: "Pets Ltd."),
            new ("user1@BigR.com", null,
                "Tenant User", tenantNameForDataKey: "Big Rocks Inc."),
        };
    }
}