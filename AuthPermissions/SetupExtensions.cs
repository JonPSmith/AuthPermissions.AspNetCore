// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;

namespace AuthPermissions
{
    /// <summary>
    /// These are a series of extension methods to register/configure the AuthPermission library
    /// </summary>
    public static class SetupExtensions
    {
        /// <summary>
        /// This is the start of registering AuthPermissions library into the available .NET DI provider
        /// This takes in the type of your enum holding your permissions and any options you want to set
        /// </summary>
        /// <typeparam name="TEnumPermissions">Must be an enum, sized as an ushort (16 bit unsigned)</typeparam>
        /// <param name="services">The DI register instance</param>
        /// <param name="options">optional: You can set certain options to change the way this library works</param>
        /// <returns></returns>
        public static AuthSetupData RegisterAuthPermissions<TEnumPermissions>(this IServiceCollection services, 
            Action<AuthPermissionsOptions> options = null) where TEnumPermissions : Enum
        {
            var authOptions = new AuthPermissionsOptions();
            options?.Invoke(authOptions);
            authOptions.InternalData.EnumPermissionsType = typeof(TEnumPermissions);
            authOptions.InternalData.EnumPermissionsType.ThrowExceptionIfEnumIsNotCorrect();

            return new AuthSetupData(services, authOptions);
        }

        /// <summary>
        /// This will register a SQL Server database to hold the AuthPermissions database.
        /// NOTE I have configured the AuthPermissionDbContext such that it can be part of another database (it has its own Migrations history table)
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static AuthSetupData UsingEfCoreSqlServer(this AuthSetupData setupData, string connectionString)
        {
            setupData.Services.AddDbContext<AuthPermissionsDbContext>(
                options =>
                {
                    options.UseSqlServer(connectionString, dbOptions =>
                        dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName));
                    EntityFramework.Exceptions.SqlServer.ExceptionProcessorExtensions.UseExceptionProcessor(options);
                });
            setupData.Options.InternalData.DatabaseType = SetupInternalData.DatabaseTypes.SqlServer;

            return setupData;
        }

        /// <summary>
        /// This registers an in-memory database. The data added to this database will be lost when the app/unit test stops
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData UsingInMemoryDatabase(this AuthSetupData setupData)
        {
            var inMemoryConnection = SetupSqliteInMemoryConnection();
            setupData.Services.AddDbContext<AuthPermissionsDbContext>(dbOptions =>
            {
                dbOptions.UseSqlite(inMemoryConnection);
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions);
            });
                
            setupData.Options.InternalData.DatabaseType = SetupInternalData.DatabaseTypes.SqliteInMemory;

            //We build a local AuthPermissionsDbContext and create the database
            var builder = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
                .UseSqlite(inMemoryConnection);
            EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(builder);
            using var context = new AuthPermissionsDbContext(builder.Options);
            context.Database.EnsureCreated();

            return setupData;
        }

        private static SqliteConnection SetupSqliteInMemoryConnection()
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = ":memory:" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);
            connection.Open();  //see https://github.com/aspnet/EntityFramework/issues/6968
            return connection;
        }

        /// <summary>
        /// This allows you to define the name of each tenant by name
        /// If you are using a hierarchical tenant design, then you must define the higher company first
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="linesOfText">If you are using a single layer then each line contains the a tenant name
        /// If you are using hierarchical tenant, then each line contains the whole hierarchy with '|' as separator, e.g.
        /// Holding company
        /// Holding company | USA branch 
        /// Holding company | USA branch | East Coast 
        /// Holding company | USA branch | East Coast | Washington
        /// Holding company | USA branch | East Coast | NewYork
        /// </param>
        /// <returns></returns>
        public static AuthSetupData AddTenantsIfEmpty(this AuthSetupData setupData, string linesOfText)
        {
            setupData.Options.InternalData.UserTenantSetupText = linesOfText ?? throw new ArgumentNullException(nameof(linesOfText));
            return setupData;
        }

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
            setupData.Options.InternalData.RolesPermissionsSetupText = linesOfText ?? throw new ArgumentNullException(nameof(linesOfText));
            return setupData;
        }

        /// <summary>
        /// This allows you to add users with their Roles and optional tenant, but only if the auth database doesn't have any AuthUsers in the database
        /// The <paramref name="userRolesSetup"/> parameter must contain a list of userId+roles.
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="userRolesSetup">A list of <see cref="DefineUserWithRolesTenant"/> containing the information on users and what auth roles they have.
        /// In this case the UserId must be filled in with the authorized users' UserId 
        /// </param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddAuthUsersIfEmpty(this AuthSetupData setupData, List<DefineUserWithRolesTenant> userRolesSetup)
        {
            setupData.Options.InternalData.UserRolesSetupData = userRolesSetup;
            return setupData;
        }
    }
}