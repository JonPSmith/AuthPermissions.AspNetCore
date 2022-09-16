﻿// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Example7.BlazorWASMandWebApi.Shared;

/// <summary>
/// This is an example of how you might build a real application
/// Notice that there are lots of permissions - the idea is to have very detailed control over your software
/// These permissions are combined to create a Role, which will be more human-focused
/// </summary>
public enum Example7Permissions : ushort //Must be ushort to work with AuthP
{
    NotSet = 0, //error condition

    //Here is an example of detailed control over some feature
    [Display(GroupName = "Stock", Name = "Read", Description = "Can read stock")]
    StockRead = 10,
    [Display(GroupName = "Stock", Name = "Add new", Description = "Can add a new stock item")]
    StockAddNew = 13,
    [Display(GroupName = "Stock", Name = "Remove", Description = "Can remove stock")]
    StockRemove = 14,

    [Display(GroupName = "Sales", Name = "Read", Description = "Can read any sales")]
    SalesRead = 20,
    [Display(GroupName = "Sales", Name = "Sell", Description = "Can sell items from stock")]
    SalesSell = 21,
    [Display(GroupName = "Sales", Name = "Return", Description = "Can return an item to stock")]
    SalesReturn = 22,

    [Display(GroupName = "Employees", Name = "Read", Description = "Can read company employees")]
    EmployeeRead = 30,

    //----------------------------------------------------
    //This is an example of what to do with permission you don't used anymore.
    //You don't want its number to be reused as it could cause problems 
    //Just mark it as obsolete and the PermissionDisplay code won't show it
    [Obsolete]
    [Display(GroupName = "Old", Name = "Not used", Description = "example of old permission")]
    OldPermissionNotUsed = 1_000,

    //----------------------------------------------------
    // A enum member with no <see cref="DisplayAttribute"/> can be used, but its not shown in the PermissionDisplay at all
    // Useful if are working on new permissions but you don't want it to be used by anyone yet 
    AnotherPermission = 2_000,

    //----------------------------------------------------
    //Admin section

    //40_000 - User admin
    [Display(GroupName = "UserAdmin", Name = "Read users", Description = "Can list User")]
    UserRead = 40_000,
    [Display(GroupName = "UserAdmin", Name = "Sync users", Description = "Syncs authorization provider with AuthUsers")]
    UserSync = 40_001,
    [Display(GroupName = "UserAdmin", Name = "Alter users", Description = "Can access the user update")]
    UserChange = 40_002,
    [Display(GroupName = "UserAdmin", Name = "Alter user's roles", Description = "Can add/remove roles from a user")]
    UserRolesChange = 40_003,
    [Display(GroupName = "UserAdmin", Name = "Move a user to another tenant", Description = "Can control what tenant they are in")]
    UserChangeTenant = 40_004,
    [Display(GroupName = "UserAdmin", Name = "Remove user", Description = "Can delete the user")]
    UserRemove = 40_005,

    //41_000 - Roles admin
    [Display(GroupName = "RolesAdmin", Name = "Read Roles", Description = "Can list Role")]
    RoleRead = 41_000,
    //This is an example of grouping multiple actions under one permission
    [Display(GroupName = "RolesAdmin", Name = "Change Role", Description = "Can create, update or delete a Role", AutoGenerateFilter = true)]
    RoleChange = 41_001,

    //41_100 - Permissions 
    [Display(GroupName = "RolesAdmin", Name = "See permissions", Description = "Can display the list of permissions", AutoGenerateFilter = true)]
    PermissionRead = 41_100,
    [Display(GroupName = "RolesAdmin", Name = "See all permissions", Description = "list will included filtered Permission ", AutoGenerateFilter = true)]
    IncludeFilteredPermissions = 41_101,

    //42_000 - tenant admin
    [Display(GroupName = "TenantAdmin", Name = "Read Tenants", Description = "Can list Tenants")]
    TenantList = 42_000,
    [Display(GroupName = "TenantAdmin", Name = "Create new Tenant", Description = "Can create new Tenants", AutoGenerateFilter = true)]
    TenantCreate = 42_001,
    [Display(GroupName = "TenantAdmin", Name = "Alter Tenants info", Description = "Can update Tenant's name", AutoGenerateFilter = true)]
    TenantUpdate = 42_002,
    [Display(GroupName = "TenantAdmin", Name = "Move tenant to another parent", Description = "Can move tenant to different parent (WARNING)", AutoGenerateFilter = true)]
    TenantMove = 42_003,
    [Display(GroupName = "TenantAdmin", Name = "Delete tenant", Description = "Can delete tenant (WARNING)", AutoGenerateFilter = true)]
    TenantDelete = 42_004,

    //43_000
    [Display(GroupName = "AppStatus", Name = "list active app statues", Description = "Can list active statues", AutoGenerateFilter = true)]
    AppStatusList = 43_000,
    [Display(GroupName = "AppStatus", Name = "Stop all users accessing app", Description = "Stop all users, apart from user who set this", AutoGenerateFilter = true)]
    AppStatusAllDown = 43_002,
    [Display(GroupName = "AppStatus", Name = "Stop users linked to specific tenant", Description = "Stop users linked to specific tenant", AutoGenerateFilter = true)]
    AppStatusTenantDown = 43_003,
    [Display(GroupName = "AppStatus", Name = "Remove an active app statue", Description = "Can turn off any active statue", AutoGenerateFilter = true)]
    AppStatusRemove = 43_005,


    //Setting the AutoGenerateFilter to true in the display allows we can exclude this permissions
    //to admin users who aren't allowed alter this permissions
    //Useful when first setting up an app
    [Display(GroupName = "SuperAdmin", Name = "AccessAll",
        Description = "This allows the user to access every feature", AutoGenerateFilter = true)]
    AccessAll = ushort.MaxValue,
}