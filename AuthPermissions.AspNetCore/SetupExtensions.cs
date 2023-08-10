// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.AccessTenantData;
using AuthPermissions.AspNetCore.AccessTenantData.Services;
using AuthPermissions.AspNetCore.GetDataKeyCode;
using AuthPermissions.AspNetCore.JwtTokenCode;
using AuthPermissions.AspNetCore.OpenIdCode;
using AuthPermissions.AspNetCore.PolicyCode;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.AspNetCore.ShardingServices.DatabaseSpecificMethods;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.BaseCode.PermissionsCode.Services;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;
using LocalizeMessagesAndErrors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;

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
        /// <param name="eventSettings">This contains the data needed to add the AuthP claims to the Azure AD login</param>
        /// <returns></returns>
        public static AuthSetupData AzureAdAuthentication(this AuthSetupData setupData, AzureAdEventSettings eventSettings)
        {
            setupData.Options.InternalData.AuthPAuthenticationType = AuthPAuthenticationTypes.OpenId;
            setupData.Services.SetupOpenAzureAdOpenId(eventSettings);

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
            setupData.Options.InternalData.RunSequentiallyOptions
                .RegisterServiceToRunInJob<StartupServiceIndividualAccountsAddSuperUser<IdentityUser>>();

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
            setupData.Options.InternalData.RunSequentiallyOptions
                .RegisterServiceToRunInJob<StartupServiceIndividualAccountsAddSuperUser<TCustomIdentityUser>>();

            return setupData;
        }

        /// <summary>
        /// This sets up the AuthP localization system, which uses the Net.LocalizeMessagesAndErrors library
        /// </summary>
        /// <typeparam name="TResource">This should be a class within your ASP.NET Core app which
        /// has .NET localization setup</typeparam>
        /// <param name="setupData"></param>
        /// <param name="supportedCultures">Provide list of supported cultures. This is used to only log
        /// missing resource entries if its supported culture. NOTE: if null, then it will log every missing culture.</param>
        /// <returns></returns>
        public static AuthSetupData SetupAuthPLocalization<TResource>(this AuthSetupData setupData,
            string[] supportedCultures)
        {
            setupData.Options.InternalData.AuthPResourceType = typeof(TResource);
            setupData.Options.InternalData.SupportedCultures = supportedCultures;

            return setupData;
        }

        /// <summary>
        /// This sets up the AuthP Sharding feature that 
        /// You must have set the <see cref="AuthPermissionsOptions.TenantType"/>  before calling this extension method
        /// </summary>
        /// <param name="setupData"></param>
        /// <param name="defaultShardingEntry">Optional: The default doesn't allows tenants being stored the AuthP database.
        /// If you want store tenants in the AuthP database, or change any other data, then provide a instance of the
        /// <see cref="ShardingEntryOptions"/> with the ctor hybridMode parameter set to true.</param>
        /// <returns></returns>
        public static AuthSetupData SetupMultiTenantSharding(this AuthSetupData setupData, 
            ShardingEntryOptions defaultShardingEntry = null)
        {
            if (!setupData.Options.TenantType.IsMultiTenant())
                throw new AuthPermissionsException(
                    $"You must define what type of multi-tenant structure you want, i.e {TenantTypes.SingleLevel} or {TenantTypes.HierarchicalTenant}.");

            setupData.Options.TenantType |= TenantTypes.AddSharding;

            if (setupData.Options.Configuration == null)
                throw new AuthPermissionsException(
                    $"You must set the {nameof(AuthPermissionsOptions.Configuration)} to the ASP.NET Core Configuration when using Sharding");

#region AuthP version 6 changes
            //This defines the default sharding entry to use when there are no entries
            //This defaults to not using the AuthP database to hold tenants
            //You need to supply a ShardingEntryOptions with the HybridMode as true
            //if you want store tenants in the AuthP database
            defaultShardingEntry ??= new ShardingEntryOptions(false);
            setupData.Services.AddSingleton(defaultShardingEntry);
#endregion

            //This gets access to the ConnectionStrings
            setupData.Services.Configure<ConnectionStringsOption>(setupData.Options.Configuration.GetSection("ConnectionStrings"));
            setupData.Services.AddTransient<ILinkToTenantDataService, LinkToTenantDataService>();

#region AuthP version 6 changes
            //This changed in version 6 of the AuthP library
            //The GetSetShardingEntriesFileStoreCache handles reading back an ShardingEntry that was undated during the same HTTP request
            //This change is because IOptionsMonitor service won't get a change to the json file until an new HTTP request has happened 
            setupData.Services.AddTransient<IGetSetShardingEntries, GetSetShardingEntriesFileStoreCache>();
            //New version service that makes it easier to create / delete tenants when using sharding
            setupData.Services.AddTransient<IShardingOnlyTenantAddRemove, ShardingOnlyTenantAddRemove>();
#endregion

            switch (setupData.Options.LinkToTenantType)
            {
                case LinkToTenantTypes.OnlyAppUsers:
                    setupData.Services
                        .AddScoped<IGetShardingDataFromUser, GetShardingDataUserAccessTenantData>();
                    break;
                case LinkToTenantTypes.AppAndHierarchicalUsers:
                    setupData.Services
                        .AddScoped<IGetShardingDataFromUser,
                            GetShardingDataAppAndHierarchicalUsersAccessTenantData>();
                    break;
                default:
                    setupData.Services.AddScoped<IGetShardingDataFromUser, GetShardingDataUserNormal>();
                    break;
            }

            //This sets up a single IDatabaseSpecificMethods for shading using your selected database.
            //There are few alternatives
            //1. If you want your shard tenants using a different database, e.g. Postgres for AuthPermissionsDbContext but SqlServer for tenants
            //2. You can have multiple database types for sharding, e.g. Postgres and SqlServer
            //3. If you are using the custom database feature you should remove the switch and select your custom IDatabaseSpecificMethods service
            switch (setupData.Options.InternalData.AuthPDatabaseType)
            {
                case AuthPDatabaseTypes.NotSet:
                    throw new AuthPermissionsException("You must define what database type you will be using.");
                case AuthPDatabaseTypes.SqliteInMemory:
                    setupData.Services.AddScoped<IDatabaseSpecificMethods, SqliteInMemorySpecificMethods>();
                    break;
                case AuthPDatabaseTypes.SqlServer:
                    setupData.Services.AddScoped<IDatabaseSpecificMethods, SqlServerDatabaseSpecificMethods>();
                    break;
                case AuthPDatabaseTypes.PostgreSQL:
                    setupData.Services.AddScoped<IDatabaseSpecificMethods, PostgresDatabaseSpecificMethods>();
                    break;
                case AuthPDatabaseTypes.CustomDatabase:
                    throw new AuthPermissionsException(
                        $"If you are using a custom database you must build your own version of the {nameof(SetupMultiTenantSharding)} extension method."); ;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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
        /// <param name="optionsAction">You can your own startup services by adding them to the <see cref="RunSequentiallyOptions"/> options.
        /// Your startup services will be registered after the Migrate the AuthP's database and bulk load process, so set the OrderNum in
        /// your startup services to a negative to get them before the AuthP startup services</param>
        public static void SetupAspNetCoreAndDatabase(this AuthSetupData setupData,
            Action<RunSequentiallyOptions> optionsAction = null)
        {
            setupData.CheckDatabaseTypeIsSet();

            setupData.RegisterCommonServices();

            if (setupData.Options.InternalData.AuthPDatabaseType != AuthPDatabaseTypes.SqliteInMemory)
                //Only run the migration on the AuthP's database if its not a in-memory database
                setupData.Options.InternalData.RunSequentiallyOptions
                    .RegisterServiceToRunInJob<StartupServiceMigrateAuthPDatabase>();

            if (!(setupData.Options.InternalData.RolesPermissionsSetupData == null || !setupData.Options.InternalData.RolesPermissionsSetupData.Any()) ||
                !(setupData.Options.InternalData.TenantSetupData == null || !setupData.Options.InternalData.TenantSetupData.Any()) ||
                !(setupData.Options.InternalData.UserRolesSetupData == null || !setupData.Options.InternalData.UserRolesSetupData.Any()))
                //Only run this if there is some Bulk Load data to apply
                setupData.Options.InternalData.RunSequentiallyOptions
                    .RegisterServiceToRunInJob<StartupServiceBulkLoadAuthPInfo>();

            optionsAction?.Invoke(setupData.Options.InternalData.RunSequentiallyOptions);
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
            var contextOptions = serviceProvider.GetRequiredService<DbContextOptions<AuthPermissionsDbContext>>();
            //This creates an AuthP database instance without any event change listeners
            var context = new AuthPermissionsDbContext(contextOptions);
            context.Database.EnsureCreated();

            var findUserIdService = serviceProvider.GetService<IAuthPServiceFactory<IFindUserInfoService>>();

            var status = await context.SeedRolesTenantsUsersIfEmpty(setupData.Options, findUserIdService);

            status.IfErrorsTurnToException();

            return serviceProvider;
        }

        //------------------------------------------------
        // private methods

        private static void RegisterCommonServices(this AuthSetupData setupData)
        {
            //common tests
            setupData.CheckThatAuthorizationTypeIsSetIfNotInUnitTestMode();

            //AuthP services
            setupData.Services.AddSingleton(setupData.Options);
            setupData.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
            setupData.Services.AddSingleton<IAuthorizationHandler, PermissionPolicyHandler>();
            setupData.Services.AddScoped<IClaimsCalculator, ClaimsCalculator>();
            setupData.Services.AddTransient<IUsersPermissionsService, UsersPermissionsService>();
            setupData.Services.AddTransient<IEncryptDecryptService, EncryptDecryptService>();
            if (setupData.Options.TenantType.IsMultiTenant())
                SetupMultiTenantServices(setupData);

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

            //Localization services
            //NOTE: If you want to use the localization services you need to setup / register the .NET IStringLocalizer<TResource> service
            setupData.Services.RegisterDefaultLocalizer("en", setupData.Options.InternalData.SupportedCultures);
            setupData.Services.AddSingleton<IAuthPDefaultLocalizer, AuthPDefaultLocalizer>();

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

        private static void SetupMultiTenantServices(AuthSetupData setupData)
        {
            //This sets up the code to get the DataKey to the application's DbContext

            //Check the TenantType and LinkToTenantType for incorrect versions
            if (!setupData.Options.TenantType.IsHierarchical()
                && setupData.Options.LinkToTenantType == LinkToTenantTypes.AppAndHierarchicalUsers)
                throw new AuthPermissionsException(
                    $"You can't set the {nameof(AuthPermissionsOptions.LinkToTenantType)} to " +
                    $"{nameof(LinkToTenantTypes.AppAndHierarchicalUsers)} unless you are using AuthP's hierarchical multi-tenant setup.");

            //The "Access the data of other tenant" feature is turned on so register the services

            //And register the service that manages the cookie and the service to start/stop linking
            setupData.Services.AddScoped<IAccessTenantDataCookie, AccessTenantDataCookie>();
            setupData.Services.AddScoped<ILinkToTenantDataService, LinkToTenantDataService>();
            if (setupData.Options.TenantType.IsSharding())
            {

            }
            else
            {
                setupData.Services.AddScoped<IGetDataKeyFromUser, GetDataKeyFromUserNormal>();

                switch (setupData.Options.LinkToTenantType)
                {
                    case LinkToTenantTypes.OnlyAppUsers:
                        setupData.Services.AddScoped<IGetDataKeyFromUser, GetDataKeyFromAppUserAccessTenantData>();
                        break;
                    case LinkToTenantTypes.AppAndHierarchicalUsers:
                        setupData.Services
                            .AddScoped<IGetDataKeyFromUser, GetDataKeyFromAppAndHierarchicalUsersAccessTenantData>();
                        break;
                    default:
                        setupData.Services.AddScoped<IGetDataKeyFromUser, GetDataKeyFromUserNormal>();
                        break;
                }
            }
        }
    }
}