// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupParts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StatusGeneric;

namespace AuthPermissions
{
    public static class SetupExtensions
    {
        public static RegisterData RegisterAuthPermissions<TEnumPermissions>(this IServiceCollection services, 
            AuthPermissionsOptions options = null) where TEnumPermissions : Enum
        {
            options ??= new AuthPermissionsOptions();

            options.EnumPermissionsType = typeof(TEnumPermissions);
            //This is needed by the ASP.NET Core policy 
            services.AddSingleton(new EnumTypeService(typeof(TEnumPermissions)));

            return new RegisterData(services, options);
        }

        public static RegisterData UsingEfCoreSqlServer(this RegisterData regData, string connectionString)
        {
            regData.Services.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlServer(connectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(PermissionConstants.MigrationsHistoryTableName)));

            return regData;
        }

        //public static RegisterData AddTenantsIfEmpty(this RegisterData regData, string linesOfText)
        //{
        //    return regData;
        //}

        /// <summary>
        /// This allows you to add Roles with their permissions, but only if the auth database contains NO RoleToPermissions
        /// </summary>
        /// <param name="regData"></param>
        /// <param name="linesOfText">This contains the lines of text, each line defined a Role with Permissions. The format is
        /// RoleName |optional-description|: PermissionName, PermissionName, PermissionName... and so on
        /// For example:
        /// SalesManager |Can authorize and alter sales|: SalesRead, SalesAdd, SalesUpdate, SalesAuthorize
        /// </param>
        /// <returns></returns>
        public static RegisterData AddRolesPermissionsIfEmpty(this RegisterData regData, string linesOfText)
        {
            regData.RolesPermissionsSetupText = linesOfText;
            return regData;
        }

        /// <summary>
        /// This allows you to define permission user, but only if the auth database doesn't have any UserToRoles in the database
        /// NOTE: You need the user's ID from the authentication part of your application.
        /// </summary>
        /// <param name="regData"></param>
        /// <param name="userSetup"></param>
        /// <returns></returns>
        public static RegisterData AddUsersIfEmpty(this RegisterData regData, List<DefineUserWithRolesTenant> userSetup)
        {
            regData.UsersWithRolesSetupData = userSetup;
            return regData;
        }

        public static RegisterData SetupForUnitTesting(this RegisterData regData)
        {
            var inMemoryConnection = SetupSqliteInMemoryConnection();
            regData.Services.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlite(inMemoryConnection));


            var serviceProvider = regData.Services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            var status = context.SetupInMemoryDatabase(regData);

            if (status.HasErrors)
                throw new InvalidOperationException(status.Errors.Count() == 1
                    ? status.Errors.Single().ToString()
                    : $"There were {status.Errors.Count()}:{Environment.NewLine}{status.GetAllErrors()}");

            return regData;
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

        private static IStatusGeneric SetupInMemoryDatabase(this AuthPermissionsDbContext context, RegisterData regData)
        {
            context.Database.EnsureCreated();

            var setupRoles = new SetupRolesService(context);
            var status = setupRoles.AddRolesToDatabaseIfEmpty(regData.RolesPermissionsSetupText,
                regData.Options.EnumPermissionsType);
            if (status.HasErrors)
                return status;
            
            context.SaveChanges();

            var setupUsers = new SetupUsersService(context);
            status = setupUsers.AddUsersToDatabaseIfEmpty(regData.UsersWithRolesSetupData);

            if (status.HasErrors)
                return status;

            context.SaveChanges();

            return status;
        }
    }
}