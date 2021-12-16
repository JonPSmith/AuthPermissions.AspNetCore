// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;

namespace AuthPermissions.AspNetCore.StartupServices
{
    /// <summary>
    /// This seeds the AuthP database with roles, tenants, and users using AuthP's bulk load feature.
    /// This allows you to provide a starting point for a new application, 
    /// e.g. setting up an super admin role and a super admin user so that you can 
    /// </summary>
    public class StartupServiceBulkLoadAuthPInfo : IStartupServiceToRunSequentially
    {
        /// <summary>
        /// Sets the order. Default is zero. If multiple classes have same OrderNum, then run in the order they were registered
        /// </summary>
        public int OrderNum { get; } //The order of this startup services is defined by the order it registered

        /// <summary>
        /// This takes data from the bulk load extention methods and and if there is no data for a
        /// each type of bulk load (i.e. roles, tenants, and users), then it will write the bulk load
        /// data to the AuthP's database
        /// </summary>
        /// <param name="scopedServices">This should be a scoped service</param>
        /// <returns></returns>
        public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
        {
            var context = scopedServices.GetRequiredService<AuthPermissionsDbContext>();
            var authOptions = scopedServices.GetRequiredService<AuthPermissionsOptions>();
            var findUserIdServiceFactory = scopedServices.GetRequiredService<IAuthPServiceFactory<IFindUserInfoService>>();

            var status = await context.SeedRolesTenantsUsersIfEmpty(authOptions, findUserIdServiceFactory);
            status.IfErrorsTurnToException();
        }
    }
}