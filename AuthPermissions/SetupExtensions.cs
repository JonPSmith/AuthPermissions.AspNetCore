// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupParts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

        public static AuthSetupData UsingEfCoreSqlServer(this AuthSetupData setupData, string connectionString)
        {
            setupData.Services.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlServer(connectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(PermissionConstants.MigrationsHistoryTableName)));
            setupData.DatabaseType = AuthSetupData.DatabaseTypes.SqlServer;

            return setupData;
        }

        /// <summary>
        /// This registers an in-memory database. The data added to this database will be lost when the app/unit test has been run
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData UsingInMemoryDatabase(this AuthSetupData setupData)
        {
            var inMemoryConnection = SetupSqliteInMemoryConnection();
            setupData.Services.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlite(inMemoryConnection));
            setupData.DatabaseType = AuthSetupData.DatabaseTypes.InMemory;

            return setupData;
        }

        //public static AuthSetupData AddTenantsIfEmpty(this AuthSetupData setupData, string linesOfText)
        //{
        //    return setupData;
        //}

        /// <summary>
        /// This allows you to add Roles with their permissions, but only if the auth database contains NO RoleToPermissions
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="linesOfText">This contains the lines of text, each line defined a Role with Permissions. The format is
        /// RoleName |optional-description|: PermissionName, PermissionName, PermissionName... and so on
        /// For example:
        /// SalesManager |Can authorize and alter sales|: SalesRead, SalesAdd, SalesUpdate, SalesAuthorize
        /// </param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddRolesPermissionsIfEmpty(this AuthSetupData setupData, string linesOfText)
        {
            setupData.RolesPermissionsSetupText = linesOfText;
            return setupData;
        }

        /// <summary>
        /// This allows you to define permission user, but only if the auth database doesn't have any UserToRoles in the database
        /// NOTE: You need the user's ID from the authentication part of your application.
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="userRolesSetup">A list of <see cref="DefineUserWithRolesTenant"/> containing the information on users and what auth roles they have</param>
        /// <param name="findUserId">This is a function that should provide the userId from the <see cref="DefineUserWithRolesTenant.UniqueUserName"/></param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddUsersRolesIfEmpty(this AuthSetupData setupData, List<DefineUserWithRolesTenant> userRolesSetup, 
            Func<string,string> findUserId)
        {
            setupData.UserRolesSetupData = userRolesSetup;
            setupData.FindUserId = findUserId;
            return setupData;
        }

        public static AuthSetupData SetupForUnitTesting(this AuthSetupData setupData)
        {
            if (setupData.DatabaseType != AuthSetupData.DatabaseTypes.InMemory)
                throw new InvalidOperationException(
                    $"You can only call the {nameof(SetupForUnitTesting)} if you used the {nameof(UsingInMemoryDatabase)} method.");

            var serviceProvider = setupData.Services.BuildServiceProvider();
            serviceProvider.AddRoleUserToAuthDb(setupData);

            return setupData;
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