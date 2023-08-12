// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.DownStatusCode;
using Example6.MvcWebApp.Sharding.Data;
using Example6.MvcWebApp.Sharding.PermissionsCode;
using Example6.SingleLevelSharding.AppStart;
using Example6.SingleLevelSharding.EfCoreCode;
using Example6.SingleLevelSharding.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Net.DistributedFileStoreCache;
using RunMethodsSequentially;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.RegisterAuthPermissions<Example6Permissions>(options =>
{
    options.TenantType = TenantTypes.SingleLevel;
    options.EncryptionKey = builder.Configuration[nameof(AuthPermissionsOptions.EncryptionKey)];
    options.PathToFolderToLock = builder.Environment.WebRootPath;
    options.SecondPartOfShardingFile = builder.Environment.EnvironmentName;
    options.Configuration = builder.Configuration;
})
    //NOTE: This uses the same database as the individual accounts DB
    .UsingEfCoreSqlServer(connectionString)
    //AuthP version 5 and above: Use this method to configure sharding
    .SetupMultiTenantSharding(new ShardingEntryOptions(true))
    .IndividualAccountsAuthentication()
    .RegisterAddClaimToUser<AddTenantNameClaim>()
    .RegisterAddClaimToUser<AddGlobalChangeTimeClaim>()
    .RegisterTenantChangeService<ShardingTenantChangeService>()
    .AddRolesPermissionsIfEmpty(Example6AppAuthSetupData.RolesDefinition)
    .AddTenantsIfEmpty(Example6AppAuthSetupData.TenantDefinition)
    .AddAuthUsersIfEmpty(Example6AppAuthSetupData.UsersRolesDefinition)
    .RegisterFindUserInfoService<IndividualAccountUserLookup>()
    .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
    .AddSuperUserToIndividualAccounts()
    .SetupAspNetCoreAndDatabase(options =>
    {
        //Migrate individual account database
        options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
        //Add demo users to the database (if no individual account exist)
        options.RegisterServiceToRunInJob<StartupServicesIndividualAccountsAddDemoUsers>();

        //Migrate the application part of the database
        options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ShardingSingleDbContext>>();
        //This seeds the invoice database (if empty)
        options.RegisterServiceToRunInJob<StartupServiceSeedShardingDbContext>();
    });

//This is used for a) hold the sharding entries and b) to set a tenant as "Down",
builder.Services.AddDistributedFileStoreCache(options =>
{
    options.WhichVersion = FileStoreCacheVersions.Class;
    options.FirstPartOfCacheFileName = "Example6CacheFileStore";
}, builder.Environment);

//manually add services from the AuthPermissions.SupportCode project
builder.Services.AddSingleton<IGlobalChangeTimeService, GlobalChangeTimeService>(); //used for "update claims on a change" feature
builder.Services.AddSingleton<IDatabaseStateChangeEvent, TenantKeyOrShardChangeService>(); //triggers the "update claims on a change" feature
builder.Services.AddTransient<ISetRemoveStatus, SetRemoveStatus>();
//AuthP version 5 and above: REMOVE THIS LINE. This now done via the SetupMultiTenantSharding extension method
//builder.Services.AddTransient<IAccessDatabaseInformation, AccessDatabaseInformation>();

builder.Services.RegisterExample6Invoices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseDownForMaintenance(TenantTypes.SingleLevel | TenantTypes.AddSharding);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
