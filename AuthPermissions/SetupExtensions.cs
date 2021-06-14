// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupCode;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions
{
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
            authOptions.EnumPermissionsType = typeof(TEnumPermissions);
            authOptions.EnumPermissionsType.ThrowExceptionIfEnumIsNotCorrect();

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
                options => options.UseSqlServer(connectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName)));
            setupData.Options.DatabaseType = AuthPermissionsOptions.DatabaseTypes.SqlServer;

            return setupData;
        }

        /// <summary>
        /// This registers an in-memory database. The data added to this database will be lost when the app/unit test stops
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData UsingInMemoryDatabase(this AuthSetupData setupData)
        {
            setupData.Services.AddDbContext<AuthPermissionsDbContext>(opt =>
                opt.UseInMemoryDatabase(nameof(AuthPermissionsDbContext)));
            setupData.Options.DatabaseType = AuthPermissionsOptions.DatabaseTypes.InMemory;

            using var serviceProvider = setupData.Services.BuildServiceProvider();
            using var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            context.Database.EnsureCreated();

            return setupData;
        }

        /// <summary>
        /// This allows you to define the name of each tenant by name
        /// If you are using a hierarchical tenant design, then the whole 
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="linesOfText">If you are using a single layer then each line contains the a tenant name
        /// If you are using hierarchical tenant, then each line contains the whole hierarchy with '|' as separator, e.g.
        /// Holding company | USA branch | East Coast | New York
        /// Holding company | USA branch | East Coast | Washington
        /// </param>
        /// <returns></returns>
        public static AuthSetupData AddTenantsIfEmpty(this AuthSetupData setupData, string linesOfText)
        {
            setupData.Options.UserTenantSetupText = linesOfText ?? throw new ArgumentNullException(nameof(linesOfText));
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
            setupData.Options.RolesPermissionsSetupText = linesOfText ?? throw new ArgumentNullException(nameof(linesOfText));
            return setupData;
        }

        /// <summary>
        /// This allows you to add what roles a user has, but only if the auth database doesn't have any UserToRoles in the database
        /// The <paramref name="userRolesSetup"/> parameter must contain a list of userId+roles.
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="userRolesSetup">A list of <see cref="DefineUserWithRolesTenant"/> containing the information on users and what auth roles they have.
        /// In this case the UserId must be filled in with the authorized users' UserId 
        /// </param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddUsersRolesIfEmpty(this AuthSetupData setupData, List<DefineUserWithRolesTenant> userRolesSetup)
        {
            var badUserIds = userRolesSetup.Where(x => x.UserId == null).ToList();
            if (badUserIds.Any())
                throw new ArgumentException($"{badUserIds.Count} user definitions didn't have a UserId. " +
                                            $"Use the {nameof(AddUsersRolesIfEmptyWithUserIdLookup)} method with a usersId lookup service. Here are the name of the username without a UserId" +
                                            Environment.NewLine + string.Join(", ", badUserIds.Select(x => x.UserName))
                    ,nameof(userRolesSetup));

            if (setupData.Options.UserRolesSetupData != null)
                throw new ArgumentException(
                    $"The data has already been set. Did you already call this method or the {nameof(AddUsersRolesIfEmptyWithUserIdLookup)} method?",
                    nameof(userRolesSetup));

            setupData.Options.UserRolesSetupData = userRolesSetup;
            return setupData;
        }

        /// <summary>
        /// TThis allows you to add what roles a user has, but only if the auth database doesn't have any UserToRoles in the database
        /// It uses the <see cref="TUserLookup"/> service to look up UserIds for user definitions that have a null UserId
        /// This allows you add users+roles with a service to link your users to the AuthPermission's UserToRole
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="userRolesSetup">A list of <see cref="DefineUserWithRolesTenant"/> containing the information on users and what auth roles they have.
        /// If the UserId in the given data is null, then it will 
        /// </param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddUsersRolesIfEmptyWithUserIdLookup<TUserLookup>(this AuthSetupData setupData, 
            List<DefineUserWithRolesTenant> userRolesSetup) where TUserLookup : class, IFindUserIdService
        {
            if (setupData.Options.UserRolesSetupData != null)
                throw new ArgumentException(
                    $"The data has already been set. Did you already call this method or the {nameof(AddUsersRolesIfEmpty)} method?",
                    nameof(userRolesSetup));

            setupData.Options.UserRolesSetupData = userRolesSetup;
            setupData.Services.AddScoped<IFindUserIdService, TUserLookup>();
            return setupData;
        }

        /// <summary>
        /// This will set up the basic AppPermissions parts and and any roles, tenants and users in the in-memory database
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static async Task<AuthPermissionsDbContext> SetupForUnitTestingAsync(this AuthSetupData setupData)
        {
            if (setupData.Options.DatabaseType != AuthPermissionsOptions.DatabaseTypes.InMemory)
                throw new AuthPermissionsException(
                    $"You can only call the {nameof(SetupForUnitTestingAsync)} if you used the {nameof(UsingInMemoryDatabase)} method.");
            
            var serviceProvider = setupData.Services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            var findUserIdService = serviceProvider.GetService<IFindUserIdService>(); //Can be null
            var roleLoader = new BulkLoadRolesService(context);
            var status = roleLoader.AddRolesToDatabaseIfEmpty(setupData.Options.RolesPermissionsSetupText,
                setupData.Options.EnumPermissionsType);
            if (status.IsValid)
            {
                var tenantLoader = new BulkLoadTenantsService(context);
                status = await tenantLoader.AddTenantsToDatabaseIfEmptyAsync(setupData.Options.UserTenantSetupText,
                    setupData.Options);
            }
            if (status.IsValid)
            {
                var userLoader = new BulkLoadUsersService(context, findUserIdService, setupData.Options);
                status = await userLoader.AddUsersRolesToDatabaseIfEmptyAsync(setupData.Options.UserRolesSetupData);
            }

            status.IfErrorsTurnToException();

            return context;
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