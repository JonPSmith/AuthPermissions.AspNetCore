// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services.Internal;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode.Factories;
using StatusGeneric;

namespace AuthPermissions.SetupCode
{
    /// <summary>
    /// This adds roles/permissions, tenants and Users only if the database is empty
    /// </summary>
    public static class BulkLoadOnStartup
    {
        /// <summary>
        /// This adds roles/permissions, tenants and Users, but only if each roles/tenants/Users are empty
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="findUserInfoServiceFactory"></param>
        /// <returns></returns>
        public static async Task<IStatusGeneric> SeedRolesTenantsUsersIfEmpty(this AuthPermissionsDbContext context,
            AuthPermissionsOptions options,
            IAuthPServiceFactory<IFindUserInfoService> findUserInfoServiceFactory)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (options == null) throw new ArgumentNullException(nameof(options));

            IStatusGeneric status = new StatusGenericHandler();
            if (!context.RoleToPermissions.Any())
            {
                var roleLoader = new BulkLoadRolesService(context, options);
                status = await roleLoader.AddRolesToDatabaseAsync(options.InternalData.RolesPermissionsSetupData);
            }

            if (status is { IsValid: true } && options.TenantType.IsMultiTenant() && !context.Tenants.Any())
            {
                var tenantLoader = new BulkLoadTenantsService(context);
                status = await tenantLoader.AddTenantsToDatabaseAsync(options.InternalData.TenantSetupData, options);
            }

            if (status is { IsValid: true } && !context.UserToRoles.Any())
            {
                var userLoader = new BulkLoadUsersService(context, findUserInfoServiceFactory, options);
                status = await userLoader.AddUsersRolesToDatabaseAsync(options.InternalData.UserRolesSetupData);
            }

            return status;
        }
    }
}