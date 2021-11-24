// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.HostedServices;
using AuthPermissions.AspNetCore.JwtTokenCode;
using AuthPermissions.AspNetCore.OpenIdCode;
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
        /// This registers the code to add AuthP's claims using IndividualAccounts
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData IndividualAccountsAuthentication(this AuthSetupData setupData)
        {
            setupData.Options.InternalData.AuthPAuthenticationType = AuthPAuthenticationTypes.IndividualAccounts;
            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, AddPermissionsToUserClaims<IdentityUser>>();

            return setupData;
        }

        /// <summary>
        /// This registers the code to add AuthP's claims using IndividualAccounts that has a custom Identity User
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData IndividualAccountsAuthentication<TCustomIdentityUser>(this AuthSetupData setupData)
            where TCustomIdentityUser : IdentityUser
        {
            setupData.Options.InternalData.AuthPAuthenticationType = AuthPAuthenticationTypes.IndividualAccounts;
            setupData.Services.AddScoped<IUserClaimsPrincipalFactory<TCustomIdentityUser>, AddPermissionsToUserClaims<TCustomIdentityUser>>();

            return setupData;
        }

        /// <summary>
        /// This registers an OpenIDConnect set up to work with Azure AD authorization
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="settings">This contains the data needed to add the AuthP claims to the Azure AD login</param>
        /// <returns></returns>
        public static AuthSetupData AzureAdAuthentication(this AuthSetupData setupData, AzureAdSettings settings)
        {
            setupData.Options.InternalData.AuthPAuthenticationType = AuthPAuthenticationTypes.OpenId;
            setupData.Services.SetupOpenAzureAdOpenId(settings);

            return setupData;
        }

        /// <summary>
        /// This says you have manually set up the Authentication code which adds the AuthP Roles and Tenant claims to the cookie or JWT Token
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData ManualSetupOfAuthentication(this AuthSetupData setupData)
        {
            setupData.Options.InternalData.AuthPAuthenticationType = AuthPAuthenticationTypes.UserProvidedAuthentication;

            return setupData;
        }

        /// <summary>
        /// This will add a single user to ASP.NET Core individual accounts identity system using data in the appsettings.json file.
        /// This is here to allow you add a super-admin user when you first start up the application on a new system
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData AddSuperUserToIndividualAccounts(this AuthSetupData setupData)
        {
            setupData.CheckAuthorizationIsIndividualAccounts();
            setupData.Services.AddHostedService<HostedIndividualAccountsAddSuperUser<IdentityUser>>();

            return setupData;
        }

        /// <summary>
        /// This will add a single user to ASP.NET Core individual accounts (with custom identity)using data in the appsettings.json file.
        /// This is here to allow you add a super-admin user when you first start up the application on a new system
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns></returns>
        public static AuthSetupData AddSuperUserToIndividualAccounts<TCustomIdentityUser>(this AuthSetupData setupData)
            where TCustomIdentityUser : IdentityUser, new()
        {
            setupData.CheckAuthorizationIsIndividualAccounts();
            setupData.Services.AddHostedService<HostedIndividualAccountsAddSuperUser<TCustomIdentityUser>>();

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
        /// 2) Run a bulk load process
        /// </summary>
        /// <param name="setupData"></param>
        public static void SetupAspNetCoreAndDatabase(this AuthSetupData setupData)
        {
            setupData.CheckDatabaseTypeIsSet();

            setupData.RegisterCommonServices();

            //These are the services that can only be run on 
            if (setupData.Options.InternalData.AuthPDatabaseType != AuthPDatabaseTypes.SqliteInMemory)
                //Only run the migration on the AuthP's database if its not a in-memory database
                setupData.Services.AddHostedService<HostedSetupAuthDatabaseOnStartup>();

            if (!string.IsNullOrEmpty(setupData.Options.InternalData.RolesPermissionsSetupText) ||
                !string.IsNullOrEmpty(setupData.Options.InternalData.UserTenantSetupText) ||
                !(setupData.Options.InternalData.UserRolesSetupData == null || !setupData.Options.InternalData.UserRolesSetupData.Any()))
                //Only run this if there is some Bulk Load data
                setupData.Services.AddHostedService<HostedStartupBulkLoadAuthPInfo>();
        }

        /// <summary>
        /// This will set up the basic AppPermissions parts and and any roles, tenants and users in the in-memory database
        /// </summary>
        /// <param name="setupData"></param>
        /// <returns>The built ServiceProvider for access to AuthP's services</returns>
        public static async Task<ServiceProvider> SetupForUnitTestingAsync(this AuthSetupData setupData)
        {
            setupData.CheckDatabaseTypeIsSetToSqliteInMemory();

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
            //common tests
            setupData.CheckThatAuthorizationTypeIsSetIfNotInUnitTestMode();

            //Internal services
            setupData.Services.AddSingleton(setupData.Options);
            setupData.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
            setupData.Services.AddSingleton<IAuthorizationHandler, PermissionPolicyHandler>();
            setupData.Services.AddScoped<IClaimsCalculator, ClaimsCalculator>();
            setupData.Services.AddTransient<IUsersPermissionsService, UsersPermissionsService>();
            if (setupData.Options.TenantType != TenantTypes.NotUsingTenants)
                setupData.Services.AddScoped<IGetDataKeyFromUser, GetDataKeyFromUser>();

            //The factories for the optional services
            setupData.Services.AddTransient<IAuthPServiceFactory<ISyncAuthenticationUsers>, SyncAuthenticationUsersFactory>();
            setupData.Services.AddTransient<IAuthPServiceFactory<IFindUserInfoService>, FindUserInfoServiceFactory>();
            setupData.Services.AddTransient<IAuthPServiceFactory<ITenantChangeService>, TenantChangeServiceFactory>();

            //Admin services
            setupData.Services.AddTransient<IAuthRolesAdminService, AuthRolesAdminService>();
            setupData.Services.AddTransient<IAuthTenantAdminService, AuthTenantAdminService>();
            setupData.Services.AddTransient<IAuthUsersAdminService, AuthUsersAdminService>();
            setupData.Services.AddTransient<IBulkLoadRolesService, BulkLoadRolesService>();
            setupData.Services.AddTransient<IBulkLoadTenantsService, BulkLoadTenantsService>();
            setupData.Services.AddTransient<IBulkLoadUsersService, BulkLoadUsersService>();

            //Other services
            setupData.Services.AddTransient<IDisableJwtRefreshToken, DisableJwtRefreshToken>();
            if (setupData.Options.ConfigureAuthPJwtToken != null)
            {
                //The user is using AuthP's TokenBuilder

                setupData.Options.ConfigureAuthPJwtToken.CheckThisJwtConfiguration()
                    .IfErrorsTurnToException();
                setupData.Services.AddTransient<ITokenBuilder, TokenBuilder>();
            }


        }
    }
}