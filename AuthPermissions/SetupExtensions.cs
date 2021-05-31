// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            if (!options.EnumPermissionsType.IsEnum)
                throw new ArgumentException("Must be an enum");
            if (Enum.GetUnderlyingType(options.EnumPermissionsType) != typeof(short))
                throw new InvalidOperationException(
                    $"The enum permissions {options.EnumPermissionsType.Name} should by 16 bits in size to work.\n" +
                    $"Please add ': short' to your permissions declaration, i.e. public enum {options.EnumPermissionsType.Name} : short " + "{...};");

            return new AuthSetupData(services, options);
        }

        public static AuthSetupData UsingEfCoreSqlServer(this AuthSetupData setupData, string connectionString)
        {
            setupData.Services.AddDbContext<AuthPermissionsDbContext>(
                options => options.UseSqlServer(connectionString, dbOptions =>
                    dbOptions.MigrationsHistoryTable(PermissionConstants.MigrationsHistoryTableName)));
            setupData.Options.DatabaseType = AuthPermissionsOptions.DatabaseTypes.SqlServer;

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
            setupData.Options.DatabaseType = AuthPermissionsOptions.DatabaseTypes.InMemory;

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
            setupData.Options.RolesPermissionsSetupText = linesOfText;
            return setupData;
        }

        /// <summary>
        /// This allows you to define permission user, but only if the auth database doesn't have any UserToRoles in the database
        /// NOTE: The <see cref="userRolesSetup"/> parameter must contain .
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="userRolesSetup">A list of <see cref="DefineUserWithRolesTenant"/> containing the information on users and what auth roles they have</param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddUsersRolesIfEmpty(this AuthSetupData setupData, List<DefineUserWithRolesTenant> userRolesSetup)
        {
            var badUserIds = userRolesSetup.Where(x => x.UserId == null).ToList();
            if (badUserIds.Any())
                throw new ArgumentException($"{badUserIds.Count} user definitions didn't have a UserId. " +
                                            $"Use the {nameof(AddUsersRolesIfEmptyWithUserIdLookup)} method with a usersId lookup service. Here are the name of the username without a UserId" +
                                            Environment.NewLine + string.Join(", ", badUserIds.Select(x => x.UserName))
                    ,nameof(userRolesSetup));

            setupData.Options.UserRolesSetupData = userRolesSetup;
            return setupData;
        }

        /// <summary>
        /// This allows you to define permission user, but only if the auth database doesn't have any UserToRoles in the database
        /// It uses the <see cref="TUserLookup"/> service to look up UserIds for user definitions that have a null UserId 
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="userRolesSetup">A list of <see cref="DefineUserWithRolesTenant"/> containing the information on users and what auth roles they have</param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddUsersRolesIfEmptyWithUserIdLookup<TUserLookup>(this AuthSetupData setupData, 
            List<DefineUserWithRolesTenant> userRolesSetup) where TUserLookup : class, IFindUserIdService
        {
            setupData.Options.UserRolesSetupData = userRolesSetup;
            setupData.Services.AddScoped<IFindUserIdService, TUserLookup>();
            return setupData;
        }

        public static async Task<AuthPermissionsDbContext> SetupForUnitTestingAsync(this AuthSetupData setupData)
        {
            if (setupData.Options.DatabaseType != AuthPermissionsOptions.DatabaseTypes.InMemory)
                throw new InvalidOperationException(
                    $"You can only call the {nameof(SetupForUnitTestingAsync)} if you used the {nameof(UsingInMemoryDatabase)} method.");
            
            var serviceProvider = setupData.Services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            var findUserIdService = serviceProvider.GetService<IFindUserIdService>();
            await context.AddRoleUserToAuthDbAsync(setupData.Options, findUserIdService);

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