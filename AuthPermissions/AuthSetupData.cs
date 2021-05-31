// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AuthPermissions.SetupParts;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
    public class AuthSetupData
    {
        public enum DatabaseTypes {NotSet, InMemory, SqlServer}

        public AuthSetupData(IServiceCollection services, AuthPermissionsOptions options)
        {
            Services = services;
            Options = options;
        }

        public IServiceCollection Services { get; }

        public AuthPermissionsOptions Options { get; }

        /// <summary>
        /// This contains the type of database used
        /// </summary>
        public DatabaseTypes DatabaseType { get; internal set; }

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
        /// This is a function provided by the application that can from the UserId based on the <see cref="DefineUserWithRolesTenant.UniqueUserName"/> 
        /// </summary>
        public Func<string, string> FindUserId { get; internal set; }

        /// <summary>
        /// This holds the definition for a user, with its various parts
        /// See the <see cref="DefineUserWithRolesTenant"/> class for information you need to provide
        /// </summary>
        public List<DefineUserWithRolesTenant> UserRolesSetupData { get; internal set; }
    }
}