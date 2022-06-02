// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Example5.MvcWebApp.AzureAdB2C.PermissionCode
{
    /// <summary>
    /// This is an example of how you might build a real application
    /// Notice that there are lots of permissions - the idea is to have very detailed control over your software
    /// These permissions are combined to create a Role, which will be more human-focused
    /// </summary>
    public enum Example5Permissions : ushort //Must be ushort to work with AuthP
    {
        NotSet = 0, //error condition

        //Here is an example of detailed control over some feature
        [Display(GroupName = "Basic", Description = "Can access BasicFeature")]
        BasicFeature = 10,




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

        [Display(GroupName = "Employees", Description = "Can invite new users to join the tenant")]
        InviteUsers = 40_100,

        //41_000 - Roles admin
        [Display(GroupName = "RolesAdmin", Name = "Read Roles", Description = "Can list Role")]
        RoleRead = 41_000,
        //This is an example of grouping multiple actions under one permission
        [Display(GroupName = "RolesAdmin", Name = "Change Role", Description = "Can create, update or delete a Role", AutoGenerateFilter = true)]
        RoleChange = 41_001,

        //41_100 - Permissions 
        [Display(GroupName = "RolesAdmin", Name = "See permissions", Description = "Can display the list of permissions", AutoGenerateFilter = true)]
        PermissionRead = 41_100,

        //Setting the AutoGenerateFilter to true in the display allows we can exclude this permissions
        //to admin users who aren't allowed alter this permissions
        //Useful for multi-tenant applications where you can set up company-level admin users where you can hide some higher-level permissions
        [Display(GroupName = "SuperAdmin", Name = "AccessAll", 
            Description = "This allows the user to access every feature", AutoGenerateFilter = true)]
        AccessAll = ushort.MaxValue,
    }
}