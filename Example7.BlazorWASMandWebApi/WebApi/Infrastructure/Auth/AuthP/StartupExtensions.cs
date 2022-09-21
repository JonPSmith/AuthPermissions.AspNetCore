using AuthPermissions.BaseCode.DataLayer;
using Example7.BlazorWASMandWebApi.Infrastructure.Persistence.Contexts;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AuthPermissions;
using Example7.BlazorWASMandWebApi.Shared;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using RunMethodsSequentially;
using AuthPermissions.AspNetCore.StartupServices;
using Net.DistributedFileStoreCache;
using AuthPermissions.SupportCode.DownStatusCode;
using Microsoft.EntityFrameworkCore;
using NetCore.AutoRegisterDi;
using GenericServices.Setup;
using Microsoft.AspNetCore.Hosting;

namespace Example7.BlazorWASMandWebApi.Infrastructure.Auth.AuthP;

public static class StartupExtensions
{
    public const string RetailDbContextHistoryName = "Retail-DbContextHistory";

    internal static IServiceCollection AddAuthP(this IServiceCollection services, ConfigurationManager configuration, IWebHostEnvironment webHostEnvironment)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        services.RegisterAuthPermissions<Example7Permissions>(options =>
        {
            options.TenantType = TenantTypes.HierarchicalTenant;
            options.PathToFolderToLock = webHostEnvironment.ContentRootPath;
        })

        //NOTE: This uses the same database as the individual accounts DB
        .UsingEfCoreSqlServer(connectionString)
        .IndividualAccountsAuthentication()
        .RegisterAddClaimToUser<AddGlobalChangeTimeClaim>()
        .RegisterTenantChangeService<RetailTenantChangeService>()
        .AddRolesPermissionsIfEmpty(Example7AppAuthSetupData.RolesDefinition)
        .AddTenantsIfEmpty(Example7AppAuthSetupData.TenantDefinition)
        .AddAuthUsersIfEmpty(Example7AppAuthSetupData.UsersRolesDefinition)
        .RegisterFindUserInfoService<IndividualAccountUserLookup>()
        .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
        .AddSuperUserToIndividualAccounts()
        .SetupAspNetCoreAndDatabase(options =>
        {
            //Migrate individual account database
            options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<AppIdentityDbContext>>();
            //Add demo users to the database
            options.RegisterServiceToRunInJob<StartupServicesIndividualAccountsAddDemoUsers>();

            //Migrate the application part of the database
            options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<RetailDbContext>>();
            //This seeds the invoice database (if empty)
            options.RegisterServiceToRunInJob<StartupServiceServiceSeedRetailDatabase>();
        });

        //This is used to set app statue as "Down" and tenant as "Down",
        //plus handling a tenant DataKey change that requires an update of the user's claims
        services.AddDistributedFileStoreCache(options =>
        {
            options.WhichVersion = FileStoreCacheVersions.Class;
            //I override the the default first part of the FileStore cache file because there are many example apps in this repo
            options.FirstPartOfCacheFileName = "Example7CacheFileStore";
        }, webHostEnvironment);

        //Have to manually register this as its in the SupportCode project
        services.AddSingleton<IGlobalChangeTimeService, GlobalChangeTimeService>(); //used for "update claims on a change" feature
        services.AddSingleton<IDatabaseStateChangeEvent, TenantKeyOrShardChangeService>(); //triggers the "update claims on a change" feature
        services.AddTransient<ISetRemoveStatus, SetRemoveStatus>(); //Used for "down for maintenance" feature  

        //This registers all the code to handle the shop part of the demo
        //Register RetailDbContext database and some services (included hosted services)
        services.RegisterExample7ShopCode(configuration);
        //Add GenericServices (after registering the RetailDbContext context)
        services.GenericServicesSimpleSetup<RetailDbContext>(Assembly.GetAssembly(typeof(StartupExtensions)));
        return services;
    }

    private static void RegisterExample7ShopCode(this IServiceCollection services, IConfiguration configuration)
    {
        //Register any services in this project
        services.RegisterAssemblyPublicNonGenericClasses()
            .Where(c => c.Name.EndsWith("Service"))  //optional
            .AsPublicImplementedInterfaces();

        //Register the retail database to the same database used for individual accounts and AuthP database
        services.AddDbContext<RetailDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"), dbOptions =>
            dbOptions.MigrationsHistoryTable(RetailDbContextHistoryName)));


    }
}

