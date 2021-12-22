// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.SetupCode;

namespace Example3.MvcWebApp.IndividualAccounts.PermissionsCode
{
    public static class Example3AppAuthSetupData
    {
        public static readonly List<BulkLoadRolesDto> RolesDefinition = new()
        {
            new("SuperAdmin", "Super admin - only use for setup", "AccessAll"),
            new("App Admin", "Overall app Admin", 
                "UserRead, UserSync, UserChange, UserRolesChange, UserChangeTenant, UserRemove, RoleRead, RoleChange, PermissionRead, IncludeFilteredPermissions, TenantList, TenantCreate, TenantUpdate"),
            //tenant roles
            new("Tenant User", "Can access invoices", "InvoiceRead, InvoiceCreate",
                RoleTypes.TenantAutoAdd),
            new("Tenant Admin", "Tenant-level admin",
                "EmployeeRead, InviteUsers, EmployeeRevokeActivate", RoleTypes.TenantAdminAdd),
            new("Enterprise", "Enterprise features", "InvoiceSum", RoleTypes.TenantAutoAdd)
        };

        public static readonly List<BulkLoadTenantDto> TenantDefinition = new()
        {
            new("4U Inc.", "Tenant User, Tenant Admin, Enterprise"),
            new("Pets Ltd.", "Tenant User, Tenant Admin"),
            new("Big Rocks Inc.", "Tenant User, Tenant Admin"),
            new("Mr single user.", "Tenant User"),
        };

        public static readonly List<BulkLoadUserWithRolesTenant> UsersRolesDefinition = new()
        {
            new ("Super@g1.com", null, "SuperAdmin"),
            new ("AppAdmin@g1.com", null, "App Admin"),
            new ("extraUser@g1.com", null, "Tenant User"),
            //Company admins.
            new ("admin@4uInc.com", null,
                "Tenant Admin,Tenant User", tenantNameForDataKey: "4U Inc."),
            //Company users: NOTE The Tenant User role is automatically added to all roles in the tenants
            new ("user1@4uInc.com", null,
                "", tenantNameForDataKey: "4U Inc."),
            new ("user2@4uInc.com", null,
                "", tenantNameForDataKey: "4U Inc."),
            new ("user1@Pets.com", null,
                "", tenantNameForDataKey: "Pets Ltd."),
            new ("user1@BigR.com", null,
                "", tenantNameForDataKey: "Big Rocks Inc."),
            new("single@User.com", null,
                "", tenantNameForDataKey: "Mr single user."),
        };
    }
}