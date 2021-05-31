// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using AuthPermissions.AspNetCore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.AspNetCore
{
    public static class SetupExtensions
    {
        public static AuthSetupData AppToAspNetCore(this AuthSetupData setupData)
        {
            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims>();

            return setupData; 
        }

        public static AuthSetupData SetupDatabaseOnStartup(this AuthSetupData setupData)
        {
            if (setupData.DatabaseType != AuthSetupData.DatabaseTypes.NotSet)
                throw new InvalidOperationException(
                    $"You must define which database type you want before you call the {nameof(SetupDatabaseOnStartup)} method.");

            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims>();
            setupData.Services.AddHostedService<SetupDatabaseOnStartup>();

            return setupData;
        }
    }
}