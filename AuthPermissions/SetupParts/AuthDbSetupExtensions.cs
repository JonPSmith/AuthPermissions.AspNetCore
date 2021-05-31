// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupParts.Internal;
using Microsoft.Extensions.DependencyInjection;
using StatusGeneric;

namespace AuthPermissions.SetupParts
{
    public static class AuthDbSetupExtensions
    {
        public static void AddRoleUserToAuthDb(this ServiceProvider serviceProvider, AuthSetupData setupData)
        {
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            var status = context.SetupRolesAndUsers(setupData);

            if (status.HasErrors)
                throw new InvalidOperationException(status.Errors.Count() == 1
                    ? status.Errors.Single().ToString()
                    : $"There were {status.Errors.Count()}:{Environment.NewLine}{status.GetAllErrors()}");
        }

        private static IStatusGeneric SetupRolesAndUsers(this AuthPermissionsDbContext context, AuthSetupData setupData)
        {
            context.Database.EnsureCreated();

            var setupRoles = new SetupRolesService(context);
            var status = setupRoles.AddRolesToDatabaseIfEmpty(setupData.RolesPermissionsSetupText,
                setupData.Options.EnumPermissionsType);
            if (status.HasErrors)
                return status;

            context.SaveChanges();

            var setupUsers = new SetupUsersService(context, setupData.FindUserId);
            status = setupUsers.AddUsersRolesToDatabaseIfEmpty(setupData.UserRolesSetupData);

            if (status.HasErrors)
                return status;

            context.SaveChanges();

            return status;
        }
    }
}