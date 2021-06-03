// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.AspNetCore.HostedServices;
using AuthPermissions.AspNetCore.PolicyCode;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.DataKeyCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupCode;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.AspNetCore
{
    public static class SetupExtensions
    {
        /// <summary>
        /// This will add a single user to ASP.NET Core individual accounts identity system using data in the appsettings.json file.
        /// This is here to allow you add a super-admin user when you first start up the application on a new system
        /// NOTE: for security reasons this will only add a new user if there isn't a user with the AccessAll permission
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData IndividualAccountsAddSuperUser(this AuthSetupData setupData)
        {
            setupData.Services.AddHostedService<IndividualAccountsAddSuperUser>();

            return setupData;
        }

        /// <summary>
        /// This will finalize the setting up of the AuthPermissions parts needed by ASP.NET Core
        /// NOTE: It assumes the AuthPermissions database has been created and has the current migration applied
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="addRolesUsersOnStartup"></param>
        public static void SetupAspNetCorePart(this AuthSetupData setupData, bool addRolesUsersOnStartup = false)
        {
            setupData.RegisterCommonServices();
            if (addRolesUsersOnStartup)
                setupData.Services.AddHostedService<AddAuthRolesUserOnStartup>();
        }

        /// <summary>
        /// This will ensure that the AuthPermissions database is created and migrated to the current settings
        /// and seeded with any new Roles, Tenants and Users that you may have included in the setup. 
        /// It also finalize the setting up of the AuthPermissions parts needed by ASP.NET Core
        /// </summary>
        /// <param name="setupData"></param>
        public static void SetupAuthDatabaseOnStartup(this AuthSetupData setupData)
        {
            if (setupData.Options.DatabaseType == AuthPermissionsOptions.DatabaseTypes.NotSet)
                throw new InvalidOperationException(
                    $"You must define which database type you want before you call the {nameof(SetupAuthDatabaseOnStartup)} method.");
            
            setupData.RegisterCommonServices();

            //NOTE: when I add the database locking I need run all of these within the lock
            setupData.Services.AddHostedService<SetupDatabaseOnStartup>();
            if (setupData.Options.TenantType != TenantTypes.NotUsingTenants)
                setupData.Services.AddHostedService<AddTenantsOnStartup>();
            setupData.Services.AddHostedService<AddAuthRolesUserOnStartup>();
        }

        private static void RegisterCommonServices(this AuthSetupData setupData)
        {
            setupData.Services.AddSingleton<IAuthPermissionsOptions>(setupData.Options);
            setupData.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
            setupData.Services.AddSingleton<IAuthorizationHandler, PermissionPolicyHandler>();
            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims>();
            setupData.Services.AddScoped<ICalcAllowedPermissions, CalcAllowedPermissions>();
            setupData.Services.AddScoped<ICalcDataKey, CalcDataKey>();
        }
    }
}