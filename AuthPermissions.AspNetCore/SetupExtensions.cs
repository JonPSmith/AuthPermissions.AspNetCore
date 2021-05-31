// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.AspNetCore.HostedServices;
using AuthPermissions.AspNetCore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.AspNetCore
{
    public static class SetupExtensions
    {

        public static AuthSetupData IndividualAccountsAddSuperUser(this AuthSetupData setupData)
        {
            setupData.Services.AddHostedService<IndividualAccountsAddSuperUser>();

            return setupData;
        }

        public static void SetupAspNetCorePart(this AuthSetupData setupData, bool addRolesUsersOnStartup = false)
        {
            setupData.Services.AddSingleton<IAuthPermissionsOptions>(setupData.Options);
            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims>();
            if (addRolesUsersOnStartup)
                setupData.Services.AddHostedService<AddAuthRolesUserOnStartup>();
        }

        public static void SetupAuthDatabaseOnStartup(this AuthSetupData setupData)
        {
            if (setupData.Options.DatabaseType == AuthPermissionsOptions.DatabaseTypes.NotSet)
                throw new InvalidOperationException(
                    $"You must define which database type you want before you call the {nameof(SetupAuthDatabaseOnStartup)} method.");

            setupData.Services.AddSingleton<IAuthPermissionsOptions>(setupData.Options);
            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims>();
            //NOTE: when I add the database provider I need to change this
            setupData.Services.AddHostedService<SetupDatabaseOnStartup>();
            setupData.Services.AddHostedService<AddAuthRolesUserOnStartup>();
        }


    }
}