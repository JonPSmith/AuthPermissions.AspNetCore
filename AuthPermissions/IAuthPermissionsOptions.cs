// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AuthPermissions.SetupParts;

namespace AuthPermissions
{
    public interface IAuthPermissionsOptions
    {
        /// <summary>
        /// This holds the type of the Enum holding the Permissions
        /// </summary>
        Type EnumPermissionsType { get; }

        /// <summary>
        /// This contains the type of database used
        /// </summary>
        AuthPermissionsOptions.DatabaseTypes DatabaseType { get; }

        /// <summary>
        /// This holds the a string containing the definition of the tenants
        /// See the <see cref="SetupExtensions.AddTenantsIfEmpty"/> method for the format of the lines
        /// </summary>
        string UserTenantSetupText { get; }

        /// <summary>
        /// This holds the a string containing the definition of the RolesToPermission database class
        /// See the <see cref="SetupExtensions.AddRolesPermissionsIfEmpty"/> method for the format of the lines
        /// </summary>
        string RolesPermissionsSetupText { get; }

        /// <summary>
        /// This holds the definition for a user, with its various parts
        /// See the <see cref="DefineUserWithRolesTenant"/> class for information you need to provide
        /// </summary>
        List<DefineUserWithRolesTenant> UserRolesSetupData { get; }
    }
}