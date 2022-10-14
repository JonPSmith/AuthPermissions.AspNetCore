// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.BaseCode.SetupCode;
using Example4.MvcWebApp.IndividualAccounts.Data;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Example4.ShopCode.AppStart;
using Example4.ShopCode.Dtos;
using Example4.ShopCode.EfCoreCode;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RunMethodsSequentially;
using System.Reflection;
using AuthPermissions.BaseCode.DataLayer;
using AuthPermissions.SupportCode.DownStatusCode;
using GenericServices.Setup;
using Net.DistributedFileStoreCache;

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
builder.Services.ConfigureApplicationCookie(options =>
{
    //this will cause all the logged-in users to have their claims to be updated if their claims are old
    //NOTE: You must register the AddGlobalChangeTimeClaim via RegisterAddClaimToUser
    options.Events.OnValidatePrincipal = SomethingChangedCookieEvent.UpdateClaimsIfSomethingChangesAsync;
});


builder.Services.RegisterAuthPermissions<Example4Permissions>(options =>
{
    options.TenantType = TenantTypes.HierarchicalTenant;
    options.PathToFolderToLock = builder.Environment.WebRootPath;
})
    //NOTE: This uses the same database as the individual accounts DB
    .UsingEfCoreSqlServer(connectionString)
    .IndividualAccountsAuthentication()
    .RegisterAddClaimToUser<AddGlobalChangeTimeClaim>()
    .RegisterTenantChangeService<RetailTenantChangeService>()
    .AddRolesPermissionsIfEmpty(Example4AppAuthSetupData.RolesDefinition)
    .AddTenantsIfEmpty(Example4AppAuthSetupData.TenantDefinition)
    .AddAuthUsersIfEmpty(Example4AppAuthSetupData.UsersRolesDefinition)
    .RegisterFindUserInfoService<IndividualAccountUserLookup>()
    .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
    .AddSuperUserToIndividualAccounts()
    .SetupAspNetCoreAndDatabase(options =>
    {
        //Migrate individual account database
        options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
        //Add demo users to the database
        options.RegisterServiceToRunInJob<StartupServicesIndividualAccountsAddDemoUsers>();

        //Migrate the application part of the database
        options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<RetailDbContext>>();
        //This seeds the invoice database (if empty)
        options.RegisterServiceToRunInJob<StartupServiceServiceSeedRetailDatabase>();
    });

//This is used to set app statue as "Down" and tenant as "Down",
//plus handling a tenant DataKey change that requires an update of the user's claims
builder.Services.AddDistributedFileStoreCache(options =>
{
    options.WhichVersion = FileStoreCacheVersions.Class;
    //I override the the default first part of the FileStore cache file because there are many example apps in this repo
    options.FirstPartOfCacheFileName = "Example4CacheFileStore";
}, builder.Environment);

//Have to manually register this as its in the SupportCode project
builder.Services.AddSingleton<IGlobalChangeTimeService, GlobalChangeTimeService>(); //used for "update claims on a change" feature
builder.Services.AddSingleton<IDatabaseStateChangeEvent, TenantKeyOrShardChangeService>(); //triggers the "update claims on a change" feature
builder.Services.AddTransient<ISetRemoveStatus, SetRemoveStatus>(); //Used for "down for maintenance" feature  

//This registers all the code to handle the shop part of the demo
//Register RetailDbContext database and some services (included hosted services)
builder.Services.RegisterExample4ShopCode(builder.Configuration);
//Add GenericServices (after registering the RetailDbContext context)
builder.Services.GenericServicesSimpleSetup<RetailDbContext>(Assembly.GetAssembly(typeof(ListSalesDto)));

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
app.UseDownForMaintenance(TenantTypes.HierarchicalTenant);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();