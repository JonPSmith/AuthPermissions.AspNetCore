// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Example7.MvcWebApp.ShardingOnly.PermissionsCode
{
    /// <summary>
    /// This is an example of how you might build a real application
    /// Notice that there are lots of permissions - the idea is to have very detailed control over your software
    /// These permissions are combined to create a Role, which will be more human-focused
    /// </summary>
    public enum Example7Permissions : ushort //Must be ushort to work with AuthP
    {
        NotSet = 0, //error condition

        //Here is an example of detailed control over some feature
        [Display(GroupName = "Invoices", Description = "Can see invoices")]
        InvoiceRead = 10,
        [Display(GroupName = "Invoices", Description = "Can create invoices")]
        InvoiceCreate = 11,

        [Display(GroupName = "Invoices", Description = "Will show sum of invoices")]
        InvoiceSum = 20,

        //Used by tenant-level admin user
        [Obsolete]
        [Display(GroupName = "Employees", Description = "Can read tenant employees")]
        EmployeeRead = 30,
        [Obsolete]
        [Display(GroupName = "Employees", Description = "Can revoke or activate a tenant employee")]
        EmployeeRevokeActivate = 31,
        
        [Display(GroupName = "Employees", Description = "Can invite new users to join the tenant")]
        InviteUsers = 32,

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
        [Display(GroupName = "TenantAdmin", Name = "Access other tenant data", Description = "Sets DataKey of user to another tenant", AutoGenerateFilter = true)]
        TenantAccessData = 42_005,

        //42_100 - sharding admin
        [Display(GroupName = "ShardingAdmin", Name = "List databases + tenants", Description = "List databases in the shardingsettings file", AutoGenerateFilter = true)]
        ListDbsWithTenants = 42_100,
        [Display(GroupName = "ShardingAdmin", Name = "Move tenant to another database", Description = "Move tenant to another database", AutoGenerateFilter = true)]
        MoveTenantDatabase = 42_101,

        [Display(GroupName = "ShardingAdmin", Name = "List databases", Description = "List sharding databases", AutoGenerateFilter = true)]
        ListDatabaseInfos = 42_110,
        [Display(GroupName = "ShardingAdmin", Name = "Add new database", Description = "Add new sharding database", AutoGenerateFilter = true)]
        AddDatabaseInfo = 42_111,
        [Display(GroupName = "ShardingAdmin", Name = "Update database info", Description = "Update sharding database info", AutoGenerateFilter = true)]
        UpdateDatabaseInfo = 42_112,
        [Display(GroupName = "ShardingAdmin", Name = "Remove database info", Description = "Remove sharding database info", AutoGenerateFilter = true)]
        RemoveDatabaseInfo = 42_113,

        //Setting the AutoGenerateFilter to true in the display allows we can exclude this permissions
        //to admin users who aren't allowed alter this permissions
        //Useful for multi-tenant applications where you can set up company-level admin users where you can hide some higher-level permissions
        [Display(GroupName = "SuperAdmin", Name = "AccessAll", 
            Description = "This allows the user to access every feature", AutoGenerateFilter = true)]
        AccessAll = ushort.MaxValue,
    }
}