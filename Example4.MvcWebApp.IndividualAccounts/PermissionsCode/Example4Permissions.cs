// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Example4.MvcWebApp.IndividualAccounts.PermissionsCode
{
    public enum Example4Permissions : ushort
    {
        NotSet = 0, //error condition

        //Here is an example of very detailed control over something
        [Display(GroupName = "Stock", Name = "Read", Description = "Can read stock")]
        StockRead = 10,
        [Display(GroupName = "Stock", Name = "Add new", Description = "Can add a new stock item")]
        StockAddNew = 13,
        [Display(GroupName = "Stock", Name = "Remove", Description = "Can remove stock")]
        StockRemove = 14,

        [Display(GroupName = "Sales", Name = "Read", Description = "Can read stock items")]
        SalesRead = 20,
        [Display(GroupName = "Sales", Name = "Sell", Description = "Can sell items from stock")]
        SalesSell = 21,
        [Display(GroupName = "Sales", Name = "Return", Description = "Can return an item to stock")]
        SalesReturn = 22,

        [Display(GroupName = "Employees", Name = "Read", Description = "Can read company employees")]
        EmployeeRead = 30,

        [Display(GroupName = "UserAdmin", Name = "Read users", Description = "Can list User")]
        UserRead = 40,
        [Display(GroupName = "UserAdmin", Name = "Add new user", Description = "Can add a new User")]
        UserAdd = 41,
        [Display(GroupName = "UserAdmin", Name = "Alter user's info", Description = "Can update the User email/name")]
        UserInfoChange = 42,
        [Display(GroupName = "UserAdmin", Name = "Alter user's roles", Description = "Can add/remove roles from a user")]
        UserRolesChange = 42,
        [Display(GroupName = "UserAdmin", Name = "Move a user to another tenant", Description = "Can control what tenant they are in")]
        UserChangeTenant = 43,
        [Display(GroupName = "UserAdmin", Name = "Remove user", Description = "Can delete the user")]
        UserRemove = 44,

        [Display(GroupName = "RolesAdmin", Name = "Read Roles", Description = "Can list Role")]
        RoleRead = 50,
        //This is an example of grouping multiple actions under one permission
        [Display(GroupName = "RolesAdmin", Name = "Change Role", Description = "Can create, update or delete a Role", AutoGenerateFilter = true)]
        RoleChange = 51,

        [Display(GroupName = "TenantAdmin", Name = "Read Tenants", Description = "Can list Tenants")]
        TenantRead = 60,
        [Display(GroupName = "TenantAdmin", Name = "Create new Tenant", Description = "Can create new Tenants", AutoGenerateFilter = true)]
        TenantCreate = 61,
        [Display(GroupName = "TenantAdmin", Name = "Alter existing Tenants", Description = "Can update or move a Tenant", AutoGenerateFilter = true)]
        TenantUpdate = 62,

        //This is an example of what to do with permission you don't used anymore.
        //You don't want its number to be reused as it could cause problems 
        //Just mark it as obsolete and the PermissionDisplay code won't show it
        [Obsolete]
        [Display(GroupName = "Old", Name = "Not used", Description = "example of old permission")]
        OldPermissionNotUsed = 100,

        /// <summary>
        /// A enum member with no <see cref="DisplayAttribute"/> can be used, but its not shown in the PermissionDisplay at all
        /// Useful if are working on new permissions but you don't want it to be used by anyone yet 
        /// </summary>
        AnotherPermission = 200,

        //Setting the AutoGenerateFilter to true in the display allows we can exclude this permissions
        //to admin users who aren't allowed alter this permissions
        //Useful for multi-tenant applications where you can set up company-level admin users where you can hide some higher-level permissions
        [Display(GroupName = "SuperAdmin", Name = "AccessAll", 
            Description = "This allows the user to access every feature", AutoGenerateFilter = true)]
        AccessAll = ushort.MaxValue,
    }
}