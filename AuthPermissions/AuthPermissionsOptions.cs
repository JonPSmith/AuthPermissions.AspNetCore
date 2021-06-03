// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AuthPermissions.SetupCode;

namespace AuthPermissions
{
    public class AuthPermissionsOptions : IAuthPermissionsOptions
    {
        /// <summary>
        /// The different database types that AuthPermissions supports
        /// </summary>
        public enum DatabaseTypes { NotSet, InMemory, SqlServer }


        //--------------------------------------------------
        //Tenant settings

        /// <summary>
        /// This defines whether tenant code is activated, and whether the
        /// multi-tenant is is a single layer, or many layers (hierarchical)
        /// </summary>
        public TenantTypes TenantType { get; set; }

        //-------------------------------------------------
        //internal set properties/handles

        /// <summary>
        /// Internal: holds the type of the Enum Permissions 
        /// </summary>
        public Type EnumPermissionsType { get; internal set; }

        /// <summary>
        /// Internal: This contains the type of database used
        /// </summary>
        public DatabaseTypes DatabaseType { get; internal set; }

        /// <summary>
        /// Internal: This holds the a string containing the definition of the tenants
        /// See the <see cref="SetupExtensions.AddTenantsIfEmpty"/> method for the format of the lines
        /// </summary>
        public string UserTenantSetupText { get; internal set; }

        /// <summary>
        /// Internal: This holds the a string containing the definition of the RolesToPermission database class
        /// See the <see cref="SetupExtensions.AddRolesPermissionsIfEmpty"/> method for the format of the lines
        /// </summary>
        public string RolesPermissionsSetupText { get; internal set; }

        /// <summary>
        /// Internal: This holds the definition for a user, with its various parts
        /// See the <see cref="DefineUserWithRolesTenant"/> class for information you need to provide
        /// </summary>
        public List<DefineUserWithRolesTenant> UserRolesSetupData { get; internal set; }
    }
}