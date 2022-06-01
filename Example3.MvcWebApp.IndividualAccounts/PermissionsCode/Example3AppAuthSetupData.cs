// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.SetupCode;

namespace Example3.MvcWebApp.IndividualAccounts.PermissionsCode
{
    public static class Example3AppAuthSetupData
    {
        public static readonly List<BulkLoadRolesDto> RolesDefinition = new()
        {
            new("SuperAdmin", "Super admin - only use for setup", "AccessAll"),
            new("App Admin", "Overall app Admin",
                "UserRead, UserSync, UserChange, UserRemove, " +
                "UserRolesChange, RoleRead, RoleChange, PermissionRead, IncludeFilteredPermissions, " +
                "TenantList, TenantCreate, TenantUpdate, UserChangeTenant, TenantAccessData"),
            new("App Support", "overall support - limited admin items",
                "UserRead, UserRolesChange, RoleRead, TenantList, TenantAccessData"),
            new("Invoice Reader", "Can read invoices", "InvoiceRead"),
            new("Invoice Creator", "Can access invoices", "InvoiceCreate"),
            //tenant roles
            new("Tenant Admin", "Tenant-level admin",
                "UserRead, UserRolesChange, RoleRead, InviteUsers", RoleTypes.TenantAdminAdd),
            new("Enterprise", "Enterprise features", "InvoiceSum", RoleTypes.TenantAutoAdd)
        };

        public static readonly List<BulkLoadTenantDto> TenantDefinition = new()
        {
            new("4U Inc.", "Tenant Admin, Enterprise"), //Enterprise
            new("Pets Ltd.", "Tenant Admin"),           //Pro
            new("Big Rocks Inc."),                    //Free
        };

        public static readonly List<BulkLoadUserWithRolesTenant> UsersRolesDefinition = new()
        {
            new ("Super@g1.com", null, "SuperAdmin"),
            new ("AppAdmin@g1.com", null, "App Admin"),
            new("AppSupport@g1.com", null, "App Support, Invoice Creator"),
            new ("extraUser@g1.com", null, "Invoice Creator"),
            //Company admins.
            new ("admin@4uInc.com", null,
                "Invoice Reader, Invoice Creator, Tenant Admin", tenantNameForDataKey: "4U Inc."),
            new("admin1@Pets.com", null,
                "Invoice Reader, Invoice Creator, Tenant Admin", tenantNameForDataKey: "Pets Ltd."),
            //Company users.
            new ("reader@4uInc.com", null,
                "Invoice Reader", tenantNameForDataKey: "4U Inc."),
            new ("creator@4uInc.com", null,
                "Invoice Creator", tenantNameForDataKey: "4U Inc."),
            new ("user1@Pets.com", null,
                "Invoice Reader, Invoice Creator", tenantNameForDataKey: "Pets Ltd."),
            new ("user1@BigR.com", null,
                "Invoice Reader, Invoice Creator", tenantNameForDataKey: "Big Rocks Inc."),
        };
    }
}