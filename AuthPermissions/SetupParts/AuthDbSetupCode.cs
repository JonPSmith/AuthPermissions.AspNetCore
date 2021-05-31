// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupParts.Internal;
using Microsoft.Extensions.DependencyInjection;
using StatusGeneric;

namespace AuthPermissions.SetupParts
{
    public static class AuthDbSetupExtensions
    {
        public static async Task AddRoleUserToAuthDbAsync(this AuthPermissionsDbContext context, IAuthPermissionsOptions options,
            IFindUserIdService findUserIdService)
        {
            var status = await context.SetupRolesAndUsers(options, findUserIdService);

            if (status.HasErrors)
                throw new InvalidOperationException(status.Errors.Count() == 1
                    ? status.Errors.Single().ToString()
                    : $"There were {status.Errors.Count()}:{Environment.NewLine}{status.GetAllErrors()}");
        }

        private static async Task<IStatusGeneric> SetupRolesAndUsers(this AuthPermissionsDbContext context, IAuthPermissionsOptions options,
            IFindUserIdService findUserIdService)
        {
            await context.Database.EnsureCreatedAsync();

            var setupRoles = new SetupRolesService(context);
            var status = setupRoles.AddRolesToDatabaseIfEmpty(options.RolesPermissionsSetupText,
                options.EnumPermissionsType);
            if (status.HasErrors)
                return status;

            await context.SaveChangesAsync();

            var setupUsers = new SetupUsersService(context, findUserIdService);
            status = await setupUsers.AddUsersRolesToDatabaseIfEmptyAsync(options.UserRolesSetupData);

            if (status.HasErrors)
                return status;

            await context.SaveChangesAsync();

            return status;
        }
    }
}