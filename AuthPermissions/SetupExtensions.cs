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
        public static RegisterData RegisterAuthPermissions<TEnumPermissions>(this IServiceCollection services, 
            AuthPermissionsOptions options = null) where TEnumPermissions : Enum
        {
            options ??= new AuthPermissionsOptions();

            //Register external Services
            //This is needed by the policy 
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

        public static RegisterData UsingInMemoryDatabaseForTesting(this RegisterData regData)
        {
            var inMemoryConnection = SetupSqliteInMemoryConnection();
            regData.Services.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlite(inMemoryConnection));

            return regData;
        }

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

        //public static RegisterData AddTenantsIfEmpty(this RegisterData regData, string linesOfText)
        //{
        //    return regData;
        //}

        public static RegisterData AddUsersIfEmpty(this RegisterData regData, List<DefineUserWithRolesTenant> userSetup)
        {
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

    }
}