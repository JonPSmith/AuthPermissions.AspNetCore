// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.SetupCode;

namespace Example3.MvcWebApp.IndividualAccounts.PermissionsCode
{
    public static class Example3AppAuthSetupData
    {
        public const string BulkLoadRolesWithPermissions = @"
SuperAdmin | Super admin - only use for setup|: AccessAll,
App Admin | Overall app Admin |: UserRead, UserSync, UserChange, UserRolesChange, UserChangeTenant, UserRemove, RoleRead, RoleChange, PermissionRead, IncludeFilteredPermissions, TenantList, TenantCreate, TenantUpdate

Tenant Admin | Tenant-level admin |: InvoiceRead, EmployeeRead, EmployeeRevokeActivate
Tenant User | Can access invoices |: InvoiceRead, InvoiceCreate";

        public const string BulkSingleTenants = @"
4U Inc.
Pets Ltd.
Big Rocks Inc.";

        public static readonly List<DefineUserWithRolesTenant> UsersRolesDefinition = new List<DefineUserWithRolesTenant>
        {
            new DefineUserWithRolesTenant("Super@g1.com", null, "SuperAdmin"),
            new DefineUserWithRolesTenant("AppAdmin@g1.com", null, "App Admin"),
            new DefineUserWithRolesTenant("extraUser@g1.com", null, "Tenant User"),
            //Company admins.
            new DefineUserWithRolesTenant("admin@4uInc.com", null,
                "Tenant Admin,Tenant User", tenantNameForDataKey: "4U Inc."),
            //Company users
            new DefineUserWithRolesTenant("user1@4uInc.com", null,
                "Tenant User", tenantNameForDataKey: "4U Inc."),
            new DefineUserWithRolesTenant("user2@4uInc.com", null,
                "Tenant User", tenantNameForDataKey: "4U Inc."),
            new DefineUserWithRolesTenant("user1@Pets.com", null,
                "Tenant User", tenantNameForDataKey: "Pets Ltd."),
            new DefineUserWithRolesTenant("user1@BigR.com", null,
                "Tenant User", tenantNameForDataKey: "Big Rocks Inc."),
        };
    }
}