// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupParts;
using AuthPermissions.SetupParts.Internal;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StatusGeneric;

namespace AuthPermissions
{
    public static class SetupExtensions
    {
        public static AuthSetupData RegisterAuthPermissions<TEnumPermissions>(this IServiceCollection services, 
            AuthPermissionsOptions options = null) where TEnumPermissions : Enum
        {
            options ??= new AuthPermissionsOptions();

            options.EnumPermissionsType = typeof(TEnumPermissions);
            //This is needed by the ASP.NET Core policy 
            services.AddSingleton(new EnumTypeService(typeof(TEnumPermissions)));

            return new AuthSetupData(services, options);
        }

        public static AuthSetupData UsingEfCoreSqlServer(this AuthSetupData setupDat, string connectionString)
        {
            setupDat.Services.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlServer(connectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(PermissionConstants.MigrationsHistoryTableName)));

            return setupDat;
        }

        //public static AuthSetupData AddTenantsIfEmpty(this AuthSetupData setupDat, string linesOfText)
        //{
        //    return setupDat;
        //}

        /// <summary>
        /// This allows you to add Roles with their permissions, but only if the auth database contains NO RoleToPermissions
        /// </summary>
        /// <param name="setupDat"></param>
        /// <param name="linesOfText">This contains the lines of text, each line defined a Role with Permissions. The format is
        /// RoleName |optional-description|: PermissionName, PermissionName, PermissionName... and so on
        /// For example:
        /// SalesManager |Can authorize and alter sales|: SalesRead, SalesAdd, SalesUpdate, SalesAuthorize
        /// </param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddRolesPermissionsIfEmpty(this AuthSetupData setupDat, string linesOfText)
        {
            setupDat.RolesPermissionsSetupText = linesOfText;
            return setupDat;
        }

        /// <summary>
        /// This allows you to define permission user, but only if the auth database doesn't have any UserToRoles in the database
        /// NOTE: You need the user's ID from the authentication part of your application.
        /// </summary>
        /// <param name="setupDat"></param>
        /// <param name="userRolesSetup">A list of <see cref="DefineUserWithRolesTenant"/> containing the information on users and what auth roles they have</param>
        /// <param name="findUserId">This is a function that should provide the userId from the <see cref="DefineUserWithRolesTenant.UniqueUserName"/></param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddUsersRolesIfEmpty(this AuthSetupData setupDat, List<DefineUserWithRolesTenant> userRolesSetup, 
            Func<string,string> findUserId)
        {
            setupDat.UserRolesSetupData = userRolesSetup;
            setupDat.FindUserId = findUserId;
            return setupDat;
        }

        public static AuthSetupData SetupForUnitTesting(this AuthSetupData setupDat)
        {
            var inMemoryConnection = SetupSqliteInMemoryConnection();
            setupDat.Services.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlite(inMemoryConnection));


            var serviceProvider = setupDat.Services.BuildServiceProvider();
            serviceProvider.AddRoleUserToAuthDb(setupDat);

            return setupDat;
        }

        //------------------------------------------------
        //private methods

        private static SqliteConnection SetupSqliteInMemoryConnection()
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);
            connection.Open();  //see https://github.com/aspnet/EntityFramework/issues/6968
            return connection;
        }


    }
}