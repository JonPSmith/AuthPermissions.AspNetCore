// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.HostedServices;
using AuthPermissions.AspNetCore.PolicyCode;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BulkLoadServices;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.PermissionsCode.Services;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AuthPermissions.AspNetCore
{
    /// <summary>
    /// A set of extension methods for creation and configuring the AuthPermissions that uses ASP.NET Core features
    /// </summary>
    public static class SetupExtensions
    {
        /// <summary>
        /// This will add a single user to ASP.NET Core individual accounts identity system using data in the appsettings.json file.
        /// This is here to allow you add a super-admin user when you first start up the application on a new system
        /// NOTE: for security reasons this will only add a new user if there isn't a user with the AccessAll permission
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData IndividualAccountsAddSuperUser(this AuthSetupData setupData)
        {
            setupData.Services.AddHostedService<IndividualAccountsAddSuperUser>();

            return setupData;
        }

        /// <summary>
        /// This will finalize the setting up of the AuthPermissions parts needed by ASP.NET Core
        /// NOTE: It assumes the AuthPermissions database has been created and has the current migration applied
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="addRolesUsersOnStartup"></param>
        public static void SetupAspNetCorePart(this AuthSetupData setupData, bool addRolesUsersOnStartup = false)
        {
            setupData.RegisterCommonServices();
            if (addRolesUsersOnStartup)
                setupData.Services.AddHostedService<AddRolesTenantsUsersIfEmptyOnStartup>();
        }

        /// <summary>
        /// This will ensure that the AuthPermissions database is created and migrated to the current settings
        /// and seeded with any new Roles, Tenants and Users that you may have included in the setup. 
        /// It also finalize the setting up of the AuthPermissions parts needed by ASP.NET Core
        /// </summary>
        /// <param name="setupData"></param>
        public static void SetupAuthDatabaseOnStartup(this AuthSetupData setupData)
        {
            if (setupData.Options.InternalData.DatabaseType == SetupInternalData.DatabaseTypes.NotSet)
                throw new InvalidOperationException(
                    $"You must define which database type you want before you call the {nameof(SetupAuthDatabaseOnStartup)} method.");
            
            setupData.RegisterCommonServices();

            if (setupData.Options.MigrateAuthPermissionsDbOnStartup == null &&
                setupData.Options.InternalData.DatabaseType != SetupInternalData.DatabaseTypes.SqliteInMemory)
                throw new AuthPermissionsException(
                    $"You have not set the {nameof(AuthPermissionsOptions.MigrateAuthPermissionsDbOnStartup)}. Your options are:{Environment.NewLine}" +
                    $"false:You will have to create/migrate the {nameof(AuthPermissionsDbContext)} database before you run your application.{Environment.NewLine}" +
                    $"true: AuthP will create/migrate the {nameof(AuthPermissionsDbContext)} database on startup.{Environment.NewLine}" +
                    $"NOTE: Letting AuthP create/migrate that database can have bad effects multiple instances of of the app are all trying to migrate the same database.");


            if (setupData.Options.InternalData.DatabaseType != SetupInternalData.DatabaseTypes.SqliteInMemory &&
                setupData.Options.MigrateAuthPermissionsDbOnStartup == true)
            {
                setupData.Services.AddHostedService<SetupDatabaseOnStartup>();
            }
            setupData.Services.AddHostedService<AddRolesTenantsUsersIfEmptyOnStartup>();
        }


        /// <summary>
        /// This will set up the basic AppPermissions parts and and any roles, tenants and users in the in-memory database
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static async Task<AuthPermissionsDbContext> SetupForUnitTestingAsync(this AuthSetupData setupData)
        {
            if (setupData.Options.InternalData.DatabaseType != SetupInternalData.DatabaseTypes.SqliteInMemory)
                throw new AuthPermissionsException(
                    $"You can only call the {nameof(SetupForUnitTestingAsync)} if you used the {nameof(AuthPermissions.SetupExtensions.UsingInMemoryDatabase)} method.");

            setupData.RegisterCommonServices();

            var serviceProvider = setupData.Services.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            context.Database.EnsureCreated();

            var findUserIdService = serviceProvider.GetService<IAuthPServiceFactory<IFindUserInfoService>>();

            var status = await context.SeedRolesTenantsUsersIfEmpty(setupData.Options, findUserIdService);

            status.IfErrorsTurnToException();

            return context;
        }

        private static void RegisterCommonServices(this AuthSetupData setupData)
        {
            //Internal services
            setupData.Services.AddSingleton<AuthPermissionsOptions>(setupData.Options);
            setupData.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
            setupData.Services.AddSingleton<IAuthorizationHandler, PermissionPolicyHandler>();
            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims>();
            setupData.Services.AddScoped<IClaimsCalculator, ClaimsCalculator>();
            setupData.Services.AddTransient<IUsersPermissionsService, UsersPermissionsService>();
            if (setupData.Options.TenantType != TenantTypes.NotUsingTenants)
                setupData.Services.AddScoped<IDataKeyFilter, GetDataKeyFilterFromUser>();

            //The factories for the optional services
            setupData.Services.AddTransient<IAuthPServiceFactory<ISyncAuthenticationUsers>, SyncAuthenticationUsersFactory>();
            setupData.Services.AddTransient<IAuthPServiceFactory<IFindUserInfoService>, FindUserInfoServiceFactory>();

            //Admin services
            setupData.Services.AddTransient<IAuthRolesAdminService, AuthRolesAdminService>();
            setupData.Services.AddTransient<IAuthTenantAdminService, AuthTenantAdminService>();
            setupData.Services.AddTransient<IAuthUsersAdminService, AuthUsersAdminService>();
            setupData.Services.AddTransient<IBulkLoadRolesService, BulkLoadRolesService>();
            setupData.Services.AddTransient<IBulkLoadTenantsService, BulkLoadTenantsService>();
            setupData.Services.AddTransient<IBulkLoadUsersService, BulkLoadUsersService>();

            //Other services
            setupData.Services.AddTransient<IDisableJwtRefreshToken, DisableJwtRefreshToken>();
        }
    }
}