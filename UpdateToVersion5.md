# Updating your code from AuthPermissions.AspNetCore 4.* to 5.0

Version 5.0.0 of the AuthPermissions.AspNetCore library (shortened to **AuthP** from now on) contains various new features (LINK TO roadmap), but this document covers **BREAKING CHANGES** in version 5.0.0, which are in _sharding_ multi-tenant applications. I purposely cause compile errors so that the breaking changes are obvious to you.

## TABLE OF CONTENT

1. The `IAccessDatabaseInformation` interface has changed and will cause an compile error.
2. Some sharding services have different properties, causing compile errors.
3. You need to use the new `SetupMultiTenantSharding` extension method to set up a hybrid / sharding.
4. If you are using the Postgres database, then you need to update the sharding information in your apps.  
5. The `IGetDatabaseForNewTenant` has changed.

The subsections below the items listed in the table of content.

### 1. The `IAccessDatabaseInformation` interface has changed

The `IAccessDatabaseInformation` interface is now called `IAccessDatabaseInformationVer5`. Before version 5.0.0 you needed register `IAccessDatabaseInformation` in your `Program` class with the following code.

```c#
builder.Services.AddTransient<IAccessDatabaseInformation, AccessDatabaseInformation>();
```

_Solution:_ remove the `IAccessDatabaseInformation` registration code and use the new `SetupMultiTenantSharding` extension method to setup the sharding.

### 2. Some sharding services have different properties

The `IAccessDatabaseInformationVer5` have new and renamed methods and properties.

_Solution:_ Look at the [Example6 ShardingController](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example6.MvcWebApp.Sharding/Controllers/ShardingController.cs) for the changed parts.

### 3. Use `SetupMultiTenantSharding` to set up sharding

Setting up a AuthP's sharding / hybrid multi-tenant application is now done by the `SetupMultiTenantSharding` extension method.

_Solution:_ add the new `SetupMultiTenantSharding` extension method to your AuthP's registration in your `Program` class.  
_NOTE: `SetupMultiTenantSharding` set up the sharding, but you still define the `options.TenantType` to `TenantTypes.SingleLevel` or `TenantTypes.HierarchicalTenant`._

### 4. Postgres database and your sharding information

Because of the new custom database feature the `DatabaseType` in your shardingsetting.json file MUST MATCH the `context.Database.ProviderName` short name, e.g. the `Microsoft.EntityFrameworkCore.SqlServer` short name is `SqlServer`

The problem with Postgres database is `Npgsql.EntityFrameworkCore.PostgreSQL` so the short name is `PostgreSQL`, not `Postgres`

_Solution:_ make sure your shardingsetting.json entries that use Postgres has the `DatabaseType` set to "PostgreSQL".  
_NOTE: If you don't change this you will have an exception with the following message._

```text
The Postgres database provider isn't supported.
```

### 5. The `IGetDatabaseForNewTenant` has changed

Its unlike you have used this service, but it has been changed to allow it to create a database. The version 4 implementation could only select an existing database, but the version 5 allows to create a database.

_Solution:_ change your implementation of the `IGetDatabaseForNewTenant` to match the new format - see the [DemoGetDatabaseForNewTenant](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions.SupportCode/ShardingServices/DemoGetDatabaseForNewTenant.cs) for an example, and another in the [AuthPermissions.CustomDatabaseExamples sharding](https://github.com/JonPSmith/AuthPermissions.CustomDatabaseExamples) repo.

END
