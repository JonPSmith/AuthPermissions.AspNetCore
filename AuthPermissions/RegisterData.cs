// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AuthPermissions.SetupParts;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
    public class RegisterData
    {
        public RegisterData(IServiceCollection services, AuthPermissionsOptions options)
        {
            Services = services;
            Options = options;
        }

        public IServiceCollection Services { get; }

        public AuthPermissionsOptions Options { get; }

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
        public List<DefineUserWithRolesTenant> UsersWithRolesSetupData { get; internal set; }

    }
}