// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace AuthPermissions.SetupCode
{
    /// <summary>
    /// This contains the data that is set during the set up the AuthPermissions
    /// </summary>
    public class SetupInternalData
    {
        /// <summary>
        /// holds the type of the Enum Permissions 
        /// </summary>
        public Type EnumPermissionsType { get; internal set; }

        /// <summary>
        /// This contains the type of database used for the AuthP database
        /// </summary>
        public AuthPDatabaseTypes AuthPDatabaseType { get; internal set; }

        /// <summary>
        /// This holds the connection string for the AuthP database.
        /// Its used by the Net.RunMethodsSequentially to get a global lock on startup
        /// </summary>
        public string AuthPConnectionString { get; internal set; }

        /// <summary>
        /// this contains the type of authorization your application uses
        /// </summary>
        public AuthPAuthenticationTypes AuthPAuthenticationType { get; set; }

        /// <summary>
        /// This is used in the AddSuperUserToIndividualAccounts to add a single user to the Individual Accounts authentication database
        /// </summary>
        public Type IdentityUserType { get; set; }

        /// <summary>
        /// This holds the a string containing the definition of the tenants
        /// See the <see cref="SetupExtensions.AddTenantsIfEmpty"/> method for the format of the lines
        /// </summary>
        public string UserTenantSetupText { get; internal set; }

        /// <summary>
        /// This holds the a string containing the definition of the RolesToPermission database class
        /// See the <see cref="SetupExtensions.AddRolesPermissionsIfEmpty"/> method for the format of the lines
        /// </summary>
        public string RolesPermissionsSetupText { get; internal set; }

        /// <summary>
        /// This holds the definition for a user, with its various parts
        /// See the <see cref="DefineUserWithRolesTenant"/> class for information you need to provide
        /// </summary>
        public List<DefineUserWithRolesTenant> UserRolesSetupData { get; internal set; }
    }
}