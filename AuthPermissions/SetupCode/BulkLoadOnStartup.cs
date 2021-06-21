// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.DataLayer.EfCode;
using StatusGeneric;

namespace AuthPermissions.SetupCode
{
    /// <summary>
    /// This adds roles/permissions, tenants and Users only if 
    /// </summary>
    public static class BulkLoadOnStartup
    {
        /// <summary>
        /// This adds roles/permissions, tenants and Users, but only if each roles/tenants/Users are empty
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="findUserInfoService"></param>
        /// <returns></returns>
        public static async Task<IStatusGeneric> SeedRolesTenantsUsersIfEmpty(this AuthPermissionsDbContext context,
            IAuthPermissionsOptions options,
            IFindUserInfoService findUserInfoService)
        {
            IStatusGeneric status = null;
            if (!context.RoleToPermissions.Any())
            {
                var roleLoader = new BulkLoadRolesService(context);
                status = await roleLoader.AddRolesToDatabaseAsync(options.RolesPermissionsSetupText,
                    options.EnumPermissionsType);
            }

            if (status is { IsValid: true } && options.TenantType != TenantTypes.NotUsingTenants && !context.Tenants.Any())
            {
                var tenantLoader = new BulkLoadTenantsService(context);
                status = await tenantLoader.AddTenantsToDatabaseAsync(options.UserTenantSetupText, options);
            }

            if (status is { IsValid: true } && !context.UserToRoles.Any())
            {
                var userLoader = new BulkLoadUsersService(context, findUserInfoService, options);
                status = await userLoader.AddUsersRolesToDatabaseAsync(options.UserRolesSetupData);
            }

            return status;
        }
    }
}