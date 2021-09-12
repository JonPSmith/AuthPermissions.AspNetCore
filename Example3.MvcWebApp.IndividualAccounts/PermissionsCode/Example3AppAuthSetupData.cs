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

Tenant Admin | Tenant-level admin|: EmployeeRead, UserRead, UserSync, UserChange, RoleRead
Tenant User | Can access invoices |: InvoiceRead";

        public const string BulkSingleTenants = @"
Company1
Company2
Company3";

        public static readonly List<DefineUserWithRolesTenant> UsersRolesDefinition = new List<DefineUserWithRolesTenant>
        {
            new DefineUserWithRolesTenant("Super@g1.com", null, "SuperAdmin"),
            new DefineUserWithRolesTenant("AppAdmin@g1.com", null, "App Admin"),
            //Company admins.
            new DefineUserWithRolesTenant("user@C1.com", null,
                "Tenant User, Store Manager", tenantNameForDataKey: "Company1"),
            new DefineUserWithRolesTenant("user@C2.com", null,
                "Tenant User, Store Manager", tenantNameForDataKey: "Company2"),
            new DefineUserWithRolesTenant("user@C3.com", null,
                "Tenant User, Store Manager", tenantNameForDataKey: "Company3"),
        };
    }
}