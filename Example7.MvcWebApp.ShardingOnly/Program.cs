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
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode;
using AuthPermissions.SupportCode.AddUsersServices;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using Example7.MvcWebApp.ShardingOnly.Data;
using Example7.MvcWebApp.ShardingOnly.PermissionsCode;
using Example7.SingleLevelShardingOnly.AppStart;
using Example7.SingleLevelShardingOnly.EfCoreCode;
using Example7.SingleLevelShardingOnly.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Net.DistributedFileStoreCache;
using RunMethodsSequentially;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//You need to create a migration for the individual user accounts DbContext
//add-migration CreateIdentitySchema -Context ApplicationDbContext -OutputDir Data\Migrations
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
        options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.RegisterAuthPermissions<Example7Permissions>(options =>
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
    .SetupMultiTenantSharding(new ShardingEntryOptions(false))
    .IndividualAccountsAuthentication()
    .RegisterAddClaimToUser<AddTenantNameClaim>()
    .RegisterTenantChangeService<ShardingOnlyTenantChangeService>()
    .AddRolesPermissionsIfEmpty(Example7AppAuthSetupData.RolesDefinition)
    .AddAuthUsersIfEmpty(Example7AppAuthSetupData.UsersRolesDefinition)
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
        options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ShardingOnlyDbContext>>(); ;
    });

//manually add services from the AuthPermissions.SupportCode project
//1. Services to allow a new user to create a new tenant 
builder.Services.AddTransient<IAddNewUserManager, IndividualUserAddUserManager<IdentityUser>>();
builder.Services.AddTransient<ISignInAndCreateTenant, SignInAndCreateTenant>();
builder.Services.AddTransient<IGetDatabaseForNewTenant, DemoShardOnlyGetDatabaseForNewTenant>(); //handles sharding tenants
//2. Services to create an invite to send to someone, and the code for the user to login via the invite
builder.Services.AddTransient<IInviteNewUserService, InviteNewUserService>();

//This is used for hold the sharding entries
builder.Services.AddDistributedFileStoreCache(options =>
{
    options.WhichVersion = FileStoreCacheVersions.Class;
    //The JsonSerializerForCacheFile below isn't needed in a real app.
    //I have added this to make the json easier to read.
    options.JsonSerializerForCacheFile = new JsonSerializerOptions
    {
        //This will make the json in the FileStore json file will be easier to read
        //BUT it will be a bit slower and take up more characters
        WriteIndented = true,
        //This makes unicode chars smaller - especially useful for FileStoreCacheVersions.Class
        //see https://github.com/JonPSmith/Net.DistributedFileStoreCache/wiki/Tips-on-making-your-cache-fast#class-version---already-has-unsaferelaxedjsonescaping
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    //I override the the default first part of the FileStore cache file because there are many example apps in this repo
    options.FirstPartOfCacheFileName = "Example7CacheFileStore";
}, builder.Environment);

builder.Services.RegisterExample7Invoices(builder.Configuration);

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
