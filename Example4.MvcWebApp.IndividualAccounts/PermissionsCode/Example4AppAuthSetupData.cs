// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.SetupCode;

namespace Example4.MvcWebApp.IndividualAccounts.PermissionsCode
{
    public static class Example4AppAuthSetupData
    {

        public static readonly List<BulkLoadRolesDto> RolesDefinition = new List<BulkLoadRolesDto>()
        {
            new("SuperAdmin", "Super admin - only use for setup", "AccessAll"),
            new("App Admin", "Overall app Admin",
                "UserRead, UserSync, UserChange, UserRolesChange, UserChangeTenant, UserRemove, RoleRead, RoleChange, PermissionRead, IncludeFilteredPermissions, TenantList, TenantCreate, TenantUpdate"),
            new("Tenant Admin", "Tenant-level admin", "EmployeeRead, UserRead, UserRolesChange, RoleRead"),
            new("Tenant Director", "Company CEO, can see stock/sales and employees", "EmployeeRead, StockRead, SalesRead"),
            new("Area Manager", "Area manager - check stock and sales", "StockRead, SalesRead"),
            new("Store Manager", "Shop sales manager - full access", "StockRead, StockAddNew, StockRemove, SalesRead, SalesSell, SalesReturn"),
            new("Sales Assistant", "Shop sales Assistant - just sells", "StockRead, SalesSell"),
        };

        public static readonly List<BulkLoadTenantDto> TenantDefinition = new List<BulkLoadTenantDto>()
        {
            new("4U Inc.", null, new BulkLoadTenantDto[]
            {
                new ("West Coast", null, new BulkLoadTenantDto[]
                {
                    new ("SanFran", null, new BulkLoadTenantDto[]
                    {
                        new ("Dress4U"),
                        new ("Tie4U")
                    }),
                    new ("LA", null, new BulkLoadTenantDto[]
                    {
                        new ("Shirt4U"),
                    })
                }),
                new ("East Coast", null, new BulkLoadTenantDto[]
                {
                    new ("NY Dress4U"),
                    new ("Boston Shirt4U"),
                })
            }),
            new("Pets2 Ltd.", null, new BulkLoadTenantDto[]
            {
                new ("London", null, new BulkLoadTenantDto[]
                {
                    new ("Cats Place"),
                    new ("Kitten Place")
                }),
            })
        };

        public static readonly List<BulkLoadUserWithRolesTenant> UsersRolesDefinition = new List<BulkLoadUserWithRolesTenant>
        {
            new ("Super@g1.com", null, "SuperAdmin"),
            new ("AppAdmin@g1.com", null, "App Admin"),
            //4U Inc.
            new ("admin@4uInc.com", null,
                "Tenant Admin, Area Manager", tenantNameForDataKey: "4U Inc."),
            new ("director@4uInc.com", null,
                "Tenant Director, Area Manager", tenantNameForDataKey: "4U Inc."),
            new ("westCoastManager@4uInc.com", null,
                "Area Manager", tenantNameForDataKey: "4U Inc. | West Coast"),
            new ("eastCoastManager@4uInc.com", null,
                "Area Manager", tenantNameForDataKey: "4U Inc. | East Coast"),
            //Dress4U
            new ("Dress4UManager@4uInc.com", null,
                "Store Manager", tenantNameForDataKey: "4U Inc. | West Coast | SanFran | Dress4U"),
            new ("Dress4USales@4uInc.com", null,
                "Sales Assistant", tenantNameForDataKey: "4U Inc. | West Coast | SanFran | Dress4U"),
            //Tie4U
            new ("Tie4UManager@4uInc.com", null,
                "Store Manager", tenantNameForDataKey: "4U Inc. | West Coast | SanFran | Tie4U"),
            new ("Tie4USales@4uInc.com", null,
                "Sales Assistant", tenantNameForDataKey: "4U Inc. | West Coast | SanFran | Tie4U"),
            //Shirt4U
            new ("Shirt4UManager@4uInc.com", null,
                "Store Manager", tenantNameForDataKey: "4U Inc. | West Coast | LA | Shirt4U"),
            new ("Shirt4USales@4uInc.com", null,
                "Sales Assistant", tenantNameForDataKey: "4U Inc. | West Coast | LA | Shirt4U"),

            //Pets2 Ltd.
            new ("admin@Pets2.com", null,
                "Tenant Admin, Area Manager", tenantNameForDataKey: "Pets2 Ltd."),
            new ("director@Pets2.com", null,
                "Tenant Director, Area Manager", tenantNameForDataKey: "Pets2 Ltd."),
            //Dress4U
            new ("CatsManager@Pets2.com", null,
                "Store Manager", tenantNameForDataKey: "Pets2 Ltd. | London | Cats Place"),
            new ("CatsSales@Pets2.com", null,
                "Sales Assistant", tenantNameForDataKey: "Pets2 Ltd. | London | Cats Place"),
            //Tie4U
            new ("KittenManager@Pets2.com", null,
                "Store Manager", tenantNameForDataKey: "Pets2 Ltd. | London | Kitten Place"),
            new ("KittenSales@Pets2.com", null,
                "Sales Assistant", tenantNameForDataKey: "Pets2 Ltd. | London | Kitten Place"),
        };
    }
}