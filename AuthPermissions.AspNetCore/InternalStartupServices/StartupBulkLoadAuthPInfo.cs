// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.AspNetCore.InternalStartupServices
{
    /// <summary>
    /// This seeds the AuthP database with roles, tenants, and users using AuthP's bulk load feature.
    /// This allows you to provide a starting point for a new application, 
    /// e.g. setting up an super admin role and a super admin user so that you can 
    /// </summary>
    internal class StartupBulkLoadAuthPInfo
    {
        /// <summary>
        /// This takes data from the bulk load extention methods and and if there is no data for a
        /// each type of bulk load (i.e. roles, tenants, and users), then it will write the bulk load
        /// data to the AuthP's database
        /// </summary>
        /// <param name="scopedService">This should be a scoped service</param>
        /// <returns></returns>
        public async ValueTask StartupServiceAsync(IServiceProvider scopedService)
        {
            var context = scopedService.GetRequiredService<AuthPermissionsDbContext>();
            var authOptions = scopedService.GetRequiredService<AuthPermissionsOptions>();
            var findUserIdServiceFactory = scopedService.GetRequiredService<IAuthPServiceFactory<IFindUserInfoService>>();

            var status = await context.SeedRolesTenantsUsersIfEmpty(authOptions, findUserIdServiceFactory);
            status.IfErrorsTurnToException();
        }
    }
}