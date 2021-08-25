// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.HostedServices;
using AuthPermissions.AspNetCore.JwtTokenCode;
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
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData AddSuperUserToIndividualAccounts(this AuthSetupData setupData)
        {
            setupData.Services.AddHostedService<IndividualAccountsAddSuperUser>();

            return setupData;
        }

        /// <summary>
        /// This will finalize the setting up of the AuthPermissions parts needed by ASP.NET Core
        /// NOTE: It assumes the AuthPermissions database has been created and has the current migration applied
        /// </summary>
        /// <param name="setupData"></param>
        public static void SetupAspNetCorePart(this AuthSetupData setupData)
        {
            setupData.RegisterCommonServices();
        }

        /// <summary>
        /// This finalizes the setting up of the AuthPermissions parts needed by ASP.NET Core
        /// This may trigger code to run on startup, before ASP.NET Core active, to
        /// 1) Migrate the AuthP's database 
        /// </summary>
        /// <param name="setupData"></param>
        public static void SetupAspNetCoreAndDatabase(this AuthSetupData setupData)
        {
            if (setupData.Options.InternalData.DatabaseType == SetupInternalData.DatabaseTypes.NotSet)
                throw new AuthPermissionsException("You must register a database type for the AuthP's database.");
                
            setupData.RegisterCommonServices();

            //These are the services that can only be run on 
            if (setupData.Options.InternalData.DatabaseType != SetupInternalData.DatabaseTypes.SqliteInMemory)
                //Only run the migration on the AuthP's database if its not a in-memory database
                setupData.Services.AddHostedService<SetupAuthDatabaseOnStartup>();

            if (!string.IsNullOrEmpty(setupData.Options.InternalData.RolesPermissionsSetupText) ||
                !string.IsNullOrEmpty(setupData.Options.InternalData.UserTenantSetupText) ||
                !(setupData.Options.InternalData.UserRolesSetupData == null || !setupData.Options.InternalData.UserRolesSetupData.Any()))
                //Only run this if there is some Bulk Load data
                setupData.Services.AddHostedService<AddRolesTenantsUsersIfEmptyOnStartup>();
        }


        /// <summary>
        /// This will set up the basic AppPermissions parts and and any roles, tenants and users in the in-memory database
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns>The built ServiceProvider for access to AuthP's services</returns>
        public static async Task<ServiceProvider> SetupForUnitTestingAsync(this AuthSetupData setupData)
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

            return serviceProvider;
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
            if(setupData.Options.ConfigureAuthPJwtToken != null)
            {
                //The user is using AuthP's TokenBuilder

                setupData.Options.ConfigureAuthPJwtToken.CheckThisJwtConfiguration()
                    .IfErrorsTurnToException();
                setupData.Services.AddTransient<ITokenBuilder, TokenBuilder>();
            }


        }
    }
}