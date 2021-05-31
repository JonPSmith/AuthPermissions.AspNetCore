// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.AspNetCore
{
    public static class SetupExtensions
    {
        public static RegisterData AppToAspNetCore(this RegisterData regData)
        {
            regData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims>();

            return regData; 
        }

        public static RegisterData AddDatabaseOnStartup(this RegisterData regData)
        {
            regData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims>();

            return regData;
        }
    }
}