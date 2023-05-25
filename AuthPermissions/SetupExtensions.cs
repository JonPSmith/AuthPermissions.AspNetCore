// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using RunMethodsSequentially;

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
            authOptions.TenantType.ThrowExceptionIfTenantTypeIsWrong();
            authOptions.InternalData.EnumPermissionsType = typeof(TEnumPermissions);
            authOptions.InternalData.EnumPermissionsType.ThrowExceptionIfEnumIsNotCorrect();
            authOptions.InternalData.EnumPermissionsType.ThrowExceptionIfEnumHasMembersHaveDuplicateValues();

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
            connectionString.CheckConnectString();

            if (setupData.Options.InternalData.AuthPDatabaseType != AuthPDatabaseTypes.NotSet)
                throw new AuthPermissionsException("You have already set up a database type for AuthP.");

            setupData.Options.InternalData.AuthPDatabaseType = AuthPDatabaseTypes.SqlServer;

            setupData.Services.AddDbContext<AuthPermissionsDbContext>(
                options =>
                {
                    options.UseSqlServer(connectionString, dbOptions =>
                        dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName)
                        .MigrationsAssembly("AuthPermissions.SqlServer"));
                    EntityFramework.Exceptions.SqlServer.
                        ExceptionProcessorExtensions.UseExceptionProcessor(options);
                });

            setupData.Options.InternalData.RunSequentiallyOptions =
                setupData.Services.RegisterRunMethodsSequentially(options =>
                {
                    if (setupData.Options.UseLocksToUpdateGlobalResources)
                    {
                        if (string.IsNullOrEmpty(setupData.Options.PathToFolderToLock))
                            throw new AuthPermissionsBadDataException(
                                $"The {nameof(AuthPermissionsOptions.PathToFolderToLock)} property in the {nameof(AuthPermissionsOptions)} must be set to a " +
                                "directory that all the instances of your application can access. " +
                                "This is a backup to the SQL Server lock in cases where the database doesn't exist yet.");

                        options.AddSqlServerLockAndRunMethods(connectionString);
                        options.AddFileSystemLockAndRunMethods(setupData.Options.PathToFolderToLock);
                    }
                    else
                        options.AddRunMethodsWithoutLock();
                });

            return setupData;
        }

        /// <summary>
        /// This will register a Postgres database to hold the AuthPermissions database.
        /// NOTE I have configured the AuthPermissionDbContext such that it can be part of another database (it has its own Migrations history table)
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public static AuthSetupData UsingEfCorePostgres(this AuthSetupData setupData, string connectionString)
        {
            connectionString.CheckConnectString();

            if (setupData.Options.InternalData.AuthPDatabaseType != AuthPDatabaseTypes.NotSet)
                throw new AuthPermissionsException("You have already set up a database type for AuthP.");

            setupData.Services.AddDbContext<AuthPermissionsDbContext>(
                options =>
                {
                    options.UseNpgsql(connectionString, dbOptions =>
                        dbOptions.MigrationsHistoryTable(AuthDbConstants.MigrationsHistoryTableName)
                        .MigrationsAssembly("AuthPermissions.PostgreSql"));
                    EntityFramework.Exceptions.PostgreSQL.ExceptionProcessorExtensions.UseExceptionProcessor(options);
                });
            setupData.Options.InternalData.AuthPDatabaseType = AuthPDatabaseTypes.PostgreSQL;

            setupData.Options.InternalData.RunSequentiallyOptions =
                setupData.Services.RegisterRunMethodsSequentially(options =>
                {
                    if (setupData.Options.UseLocksToUpdateGlobalResources)
                    {
                        if (string.IsNullOrEmpty(setupData.Options.PathToFolderToLock))
                            throw new AuthPermissionsBadDataException(
                                $"The {nameof(AuthPermissionsOptions.PathToFolderToLock)} property in the {nameof(AuthPermissionsOptions)} must be set to a " +
                                "directory that all the instances of your application can access. " +
                                "This is a backup to the Postgres lock in cases where the database doesn't exist yet.");

                        options.AddPostgreSqlLockAndRunMethods(connectionString);
                        options.AddFileSystemLockAndRunMethods(setupData.Options.PathToFolderToLock);
                    }
                    else
                        options.AddRunMethodsWithoutLock();
                });

            return setupData;
        }

        /// <summary>
        /// This registers an in-memory database. The data added to this database will be lost when the app/unit test stops
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData UsingInMemoryDatabase(this AuthSetupData setupData)
        {
            if (setupData.Options.InternalData.AuthPDatabaseType != AuthPDatabaseTypes.NotSet)
                throw new AuthPermissionsException("You have already set up a database type for AuthP.");

            var inMemoryConnection = SetupSqliteInMemoryConnection();
            setupData.Services.AddDbContext<AuthPermissionsDbContext>(dbOptions =>
            {
                dbOptions.UseSqlite(inMemoryConnection);
                EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(dbOptions);
            });
                
            setupData.Options.InternalData.AuthPDatabaseType = AuthPDatabaseTypes.SqliteInMemory;

            //We build a local AuthPermissionsDbContext and create the database
            var builder = new DbContextOptionsBuilder<AuthPermissionsDbContext>()
                .UseSqlite(inMemoryConnection);
            EntityFramework.Exceptions.Sqlite.ExceptionProcessorExtensions.UseExceptionProcessor(builder);
            using var context = new AuthPermissionsDbContext(builder.Options);
            context.Database.EnsureCreated();

            setupData.Options.InternalData.RunSequentiallyOptions =
                setupData.Services.RegisterRunMethodsSequentially(options =>
                {
                    //For in-memory AuthP we can't lock on anything
                    options.AddRunMethodsWithoutLock();
                });

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
        /// This registers your service that adds one claim to the user on login
        /// and on refresh of the user's claims (e.g. when JWT Token refresh happens).
        /// NOTE: If you want to add multiple claims then call this method with a class that implements the <see cref="IClaimsAdder"/> 
        /// </summary>
        /// <typeparam name="TClaimsAdder">Your class that returns a single claim to be added to a user</typeparam>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData RegisterAddClaimToUser<TClaimsAdder>(this AuthSetupData setupData)
            where TClaimsAdder : class, IClaimsAdder
        {
            setupData.Services.AddTransient<IClaimsAdder, TClaimsAdder>();
            return setupData;
        }

        /// <summary>
        /// This allows you to define the name of each tenant using the <see cref="BulkLoadTenantDto"/> class
        /// For hierarchical tenant design you add child tenants using the <see cref="BulkLoadTenantDto.ChildrenTenants"/> property
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="tenantDefinitions">list of tenant definitions. For hierarchical tenants use the <see cref="BulkLoadTenantDto.ChildrenTenants"/> property</param>
        /// <returns></returns>
        public static AuthSetupData AddTenantsIfEmpty(this AuthSetupData setupData, List<BulkLoadTenantDto> tenantDefinitions)
        {
            setupData.Options.InternalData.TenantSetupData = tenantDefinitions;
            return setupData;
        }

        /// <summary>
        /// This allows you to add Roles with their permissions, but only if the auth database contains NO RoleToPermissions
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="rolesDefinitions">This contains a list of <see cref="BulkLoadRolesDto"/> classes defining AuthP Roles</param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddRolesPermissionsIfEmpty(this AuthSetupData setupData, List<BulkLoadRolesDto> rolesDefinitions)
        {
            setupData.Options.InternalData.RolesPermissionsSetupData = rolesDefinitions;
            return setupData;
        }

        /// <summary>
        /// This allows you to add users with their Roles and optional tenant, but only if the auth database doesn't have any AuthUsers in the database
        /// The <paramref name="userRolesSetup"/> parameter must contain a list of userId+roles.
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="userRolesSetup">A list of <see cref="BulkLoadUserWithRolesTenant"/> containing the information on users and what auth roles they have.
        /// In this case the UserId must be filled in with the authorized users' UserId 
        /// </param>
        /// <returns>AuthSetupData</returns>
        public static AuthSetupData AddAuthUsersIfEmpty(this AuthSetupData setupData, List<BulkLoadUserWithRolesTenant> userRolesSetup)
        {
            setupData.Options.InternalData.UserRolesSetupData = userRolesSetup;
            return setupData;
        }
    }
}