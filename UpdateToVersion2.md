# Updating your code from AuthPermissions.AspNetCore 1.* to 2.0

This article explains how to update an existing AuthPermissions.AspNetCore 1.* project to AuthPermissions.AspNetCore 2.0. I am assuming that you are using Visual Studio.

_NOTE: I shorten the AuthPermissions.AspNetCore library name to **AuthP** from now on._

## TABLE OF CONTENT

- **MUST DO**: Always
    - [Update your application to .NET 6.0](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion2.md#update-your-application-to-net-60)
    - [Changes to registering AuthP in ASP.NET Core](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion2.md#breaking-change-changes-to-registering-authp-in-aspnet-core)
- **MUST DO**: If multi-tenant app
    - [Need to migrate your database that uses AuthP's `DataKey`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion2.md#multi-tenant-breaking-change-need-to-migrate-your-database-that-uses-authps-datakey)
    - [The `QueryRoleToPermissions` needs a userId](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion2.md#multi-tenant-breaking-change-the-queryroletopermissions-needs-a-userid)
- OPTIONAL
    - [Building/Running your own migrate / seeding code on startup](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion2.md#2-optional-buildingrunning-your-own-migrate--seeding-code-on-startup)
    - [Bulk load or Roles and Tenants have changed](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion2.md#breaking-change-bulk-load-or-roles-and-tenants-have-changed)

## Update your application to .NET 6.0

To start, you need to [download](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) and install the correct NET 6 SDK for your development machine. You also need to [download Visual Studio 2022](https://visualstudio.microsoft.com/downloads/), as Visual Studio 2019 doesn’t support Net 6.

You then have to change the target framework in every project's .csproj file that uses the AuthP library,  ASP.NET Core or EF Core. i.e.

```XML
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
</PropertyGroup>
```

Once you have updated all your projects, then you can  update all your NuGet packages to the latest version:

- AuthP should be 2.?.?
- ASP.NET Core and EF Core should be 6.?.?

_NOTE: ASP.NET Core 6 has a new "minimal hosting" approach - read Andrew Lock's [Upgrading a .NET 5 "Startup-based" app to .NET 6](https://andrewlock.net/exploring-dotnet-6-part-12-upgrading-a-dotnet-5-startup-based-app-to-dotnet-6/) article for your options._

## BREAKING CHANGE: Changes to registering AuthP in ASP.NET Core

AuthP version 2 uses the [Net.RunMethodsSequentially](https://www.nuget.org/packages/Net.RunMethodsSequentially) library inside AuthP's `SetupAspNetCoreAndDatabase` configuration method. This allow you to migrate / seed your database(s) on startup even if you are running multiple instances in production (see [this article](https://www.thereformedprogrammer.net/how-to-safely-apply-an-ef-core-migrate-on-asp-net-core-startup/) for more info).

However this new feature does require to changes to the registering code in your Net 5 `Startup` code, and if you migrate / seed need your application's database on startup, then you will need to change how you do that.

### 1. REQUIRED: Changes to registering AuthP in ASP.NET Core

The Net.RunMethodsSequentially library needs a global resource, such as a database, to lock against. But to handle the case of the database doesn't exist it needs a second global resource, which I have chosen as a FileSystem Directory, e.g. ASP.NET Core's wwwRoot directory.

You have to provide the database connection string and the FilePath to the ASP.NET Core's wwwRoot directory via the options part of the `RegisterAuthPermissions` extension method - see the setup code  below, which was taken from [Example5's Startup class](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example5.MvcWebApp.AzureAdB2C/Startup.cs).

```c#
public void ConfigureServices(IServiceCollection services)
{
    var connectionString = _configuration.GetConnectionString("DefaultConnection");
    services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(_configuration.GetSection("AzureAd"));

    services.AddControllersWithViews();
    services.AddRazorPages()
         .AddMicrosoftIdentityUI();

    //Needed by the SyncAzureAdUsers code
    services.Configure<AzureAdOptions>(_configuration.GetSection("AzureAd"));

    services.RegisterAuthPermissions<Example5Permissions>(options =>
        {
            options.AppConnectionString = connectionString;
            options.PathToFolderToLock = _env.WebRootPath;
        })
        .AzureAdAuthentication(AzureAdSettings.AzureAdDefaultSettings(false))
        .UsingEfCoreSqlServer(connectionString)
        .AddRolesPermissionsIfEmpty(Example5AppAuthSetupData.RolesDefinition)
        .AddAuthUsersIfEmpty(Example5AppAuthSetupData.UsersRolesDefinition)
        .RegisterAuthenticationProviderReader<SyncAzureAdUsers>()
        .SetupAspNetCoreAndDatabase();
}
```

_NOTE: It you are SURE that you won't have multiple instances, then you can set the options `UseLocksToUpdateGlobalResources` property to false. This tells the  Net.RunMethodsSequentially library that it can run the startup services without obtaining a global lock. See [Example2's Startup class](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Startup.cs) for an example of setting the `UseLocksToUpdateGlobalResources` property to false._

### 2. OPTIONAL: Building/Running your own migrate / seeding code on startup

In cases where you want to migrate and/or seed your own database on startup, then you can add extra _startup services_ to the Net.RunMethodsSequentially inside AuthP. The Net.RunMethodsSequentially will each of your startup services (and the AuthP startup services) within global lock. This means if you have multiple instances of your app the startup services in each instance can't run at the same time as other startup services in another instance. But remember - each instance WILL run the startup services, so make sure your startup services check if the database has already been updated.

#### 2.a Creating startup services to run on startup of 

- If you want to run EF Core's `Migrate` method on startup, then there is the  [`StartupServiceMigrateAnyDbContext<TContext>`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions.AspNetCore/StartupServices/StartupServiceMigrateAnyDbContext.cs) class that can do that for you.
- If you want to run method on startup, say to seed a database, then you need to create a class that inherits the [`IStartupServiceToRunSequentially`](https://github.com/JonPSmith/RunStartupMethodsSequentially/blob/main/RunMethodsSequentially/IStartupServiceToRunSequentially.cs) interface. 

The code below is taken from [Example3's Startup class](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example3.MvcWebApp.IndividualAccounts/Startup.cs) and shows the `SetupAspNetCoreAndDatabase` method where you can add four extra _startup services_ that will be run on startup.

```c#
services.RegisterAuthPermissions<Example3Permissions>(options =>
    {
        options.TenantType = TenantTypes.SingleLevel;
        options.AppConnectionString = connectionString;
        options.PathToFolderToLock = _env.WebRootPath;
    })
    //NOTE: This uses the same database as the individual accounts DB
    .UsingEfCoreSqlServer(connectionString)
    .IndividualAccountsAuthentication()
    .RegisterTenantChangeService<InvoiceTenantChangeService>()
    .AddRolesPermissionsIfEmpty(Example3AppAuthSetupData.RolesDefinition)
    .AddTenantsIfEmpty(Example3AppAuthSetupData.TenantDefinition)
    .AddAuthUsersIfEmpty(Example3AppAuthSetupData.UsersRolesDefinition)
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
        options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<InvoicesDbContext>>();
        //This seeds the invoice database (if empty)
        options.RegisterServiceToRunInJob<StartupServiceSeedInvoiceDbContext>();
    });
```

## BREAKING CHANGE: Bulk load or Roles and Tenants have changed

I don't expect that many people use the Bulk Load feature, but I use it in the examples in the github.com/JonPSmith/AuthPermissions.AspNetCore repo so that you can run the examples with demo data.

Version 2 has a number of new features around Roles and Tenants which required extra data. So if you do use the Bulk Load feature, then you have to change:

1. For Roles you need to use a list of [`BulkLoadRolesDto`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions/SetupCode/BulkLoadRolesDto.cs) classes instead of a string.
2. For Tenants you need to use a list of [`BulkLoadTenantDto`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions/SetupCode/BulkLoadTenantDto.cs) classes instead of a string.

The code shown below is the Version 2 setup of [Roles and Tenants in Example3](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example3.MvcWebApp.IndividualAccounts/PermissionsCode/Example3AppAuthSetupData.cs) that matches the Version 1 setup of  [Roles and Tenants in Example3](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/Version1/Example3.MvcWebApp.IndividualAccounts/PermissionsCode/Example3AppAuthSetupData.cs).

```c#
public static readonly List<BulkLoadRolesDto> RolesDefinition = new List<BulkLoadRolesDto>()
{
    new("SuperAdmin", "Super admin - only use for setup", "AccessAll"),
    new("App Admin", "Overall app Admin", 
        "UserRead, UserSync, UserChange, UserRolesChange, UserChangeTenant, " +
        "UserRemove, RoleRead, RoleChange, PermissionRead, IncludeFilteredPermissions, " +
        "TenantList, TenantCreate, TenantUpdate"),
    new("Tenant Admin", "Tenant-level admin", "InvoiceRead, EmployeeRead, EmployeeRevokeActivate"),
    new("Tenant User", "Can access invoices", "InvoiceRead, InvoiceCreate"),
};


public static readonly List<BulkLoadTenantDto> TenantDefinition = new List<BulkLoadTenantDto>()
{
    new("4U Inc."),
    new("Pets Ltd."),
    new("Big Rocks Inc."),
};
```

## MULTI-TENANT BREAKING CHANGE: Need to migrate your database that uses AuthP's `DataKey`

While adding new multi-tenant features to this library I found a bug in the use of the `DataKey` which can cause hierarchical multi-tenant applications to sometimes access another tenant’s data. This is therefore a **critical bug**.

Version 2 of the library fixes to the bug, but does need a migrate any application's databases that use the `DataKey`. Single multi-tenant applications in version 1 didn’t have a bug, but the AuthP `DataKey` uses the same `DataKey` for Single and Hierarchical multi-tenant, so you still need to follow this information when upgrading to Version 2.

So, **if you are upgrading a version 1 Single and Hierarchical multi-tenant application that uses the `DataKey` to version 2 you need to follow these steps**:

### 1. The `DataKey` SQL type has changed, so you need to migrate any database using a `DataKey`

For performance reasons I have changed the SQL type of the `DataKey` from `nvarchar` to `varchar`, and set a size. If you are using AuthP’s  [`DataKeyQueryExtension`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions/DataLayer/EfCode/DataKeyQueryExtension.cs) extension methods, then these changes are automatically applied, and therefore will force you to create a EF Core migration.

So run the `Add-Migration` command in VS2022's Package Manager Console to create a new migration

### 2. You need to edit the new migration to add code to fix the `DataKey` format

Once you have created a new migrate your application’s database you have to manually add some code to the new migration before you run it. I have created a [Version2DataKeyHelper](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions/DataLayer/EfCode/Version2DataKeyHelper.cs) class which contains the method called `UpdateToVersion2DataKeyFormat` that you can add for each entity that has a `DataKey`. Because of the changes in step 1 the migration will contain a change to every table that has a `DataKey` columns. The code below is taken from [Example4 “Version2” migration]( https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.ShopCode/EfCoreCode/Migrations/20211215114854_Version2.cs.).

```c#
public partial class Version2 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "DataKey",
            schema: "retail",
            table: "ShopStocks",
            type: "varchar(250)",
            unicode: false,
            maxLength: 250,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(450)",
            oldNullable: true);

        //MANUALLY ADD THIS CODE FOR EVERY TABLE CONTAINING A DATAKEY
        migrationBuilder.UpdateToVersion2DataKeyFormat("retail.ShopStocks");

        migrationBuilder.AlterColumn<string>(
            name: "DataKey", //.....
        //Rest of code left out
    }
}
```

As you can see in the code above, I manually added the call to the `UpdateToVersion2DataKeyFormat` extension method, with the name of the table at a parameter, for **every table that has a `DataKey`**.

_NOTE: The `UpdateToVersion2DataKeyFormat` method changes the `DataKey` string to match the Version 2 format. If you want to go back to Version 1 you will need to create a similar method that returns the `DataKey` string to the Version 1 format. If you can’t work out how to do that, then open an issue and I can create one._

## MULTI-TENANT BREAKING CHANGE: The `QueryRoleToPermissions` needs a userId

In Version 2 the Roles that a Tenant Admin can see has changed, so in multi-tenant applications you now need to provide the Id of the logged-in user. This is pretty easy as shown in the code taken for [Example3's RoleController](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example3.MvcWebApp.IndividualAccounts/Controllers/RolesController.cs).

```c#
[HasPermission(Example4Permissions.RoleRead)]
public async Task<IActionResult> Index(string message)
{
    var userId = User.Claims.GetUserIdFromClaims();
    var permissionDisplay = await
        _authRolesAdmin.QueryRoleToPermissions(userId).ToListAsync();

    ViewBag.Message = message;

    return View(permissionDisplay);
}
```
 
END
