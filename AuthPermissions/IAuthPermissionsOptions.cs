// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AuthPermissions.SetupCode;

namespace AuthPermissions
{
    public interface IAuthPermissionsOptions
    {
        /// <summary>
        /// This defines whether tenant code is activated, and whether the
        /// multi-tenant is is a single layer, or many layers (hierarchical)
        /// </summary>
        TenantTypes TenantType { get; set; }

        /// <summary>
        /// Internal: holds the type of the Enum Permissions 
        /// </summary>
        Type EnumPermissionsType { get; }

        /// <summary>
        /// Internal: This contains the type of database used
        /// </summary>
        AuthPermissionsOptions.DatabaseTypes DatabaseType { get; }

        /// <summary>
        /// Internal: This holds the a string containing the definition of the tenants
        /// See the <see cref="SetupExtensions.AddTenantsIfEmpty"/> method for the format of the lines
        /// </summary>
        string UserTenantSetupText { get; }

        /// <summary>
        /// Internal: This holds the a string containing the definition of the RolesToPermission database class
        /// See the <see cref="SetupExtensions.AddRolesPermissionsIfEmpty"/> method for the format of the lines
        /// </summary>
        string RolesPermissionsSetupText { get; }

        /// <summary>
        /// Internal: This holds the definition for a user, with its various parts
        /// See the <see cref="DefineUserWithRolesTenant"/> class for information you need to provide
        /// </summary>
        List<DefineUserWithRolesTenant> UserRolesSetupData { get; }
    }
}