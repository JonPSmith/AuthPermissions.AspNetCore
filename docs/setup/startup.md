# Setting up the AuthPermissions in ASP.NET Core

Like all ASP.NET Core libraries the AuthP library has to be registered as a service. Also, yhe AuthP library follows the ASP.NET Core of format of configuring itself with extension methods, and providing configuration points.

Here is a simple example of what registering the AuthP library would look like, but there are lots of different extension methods and configurations.

```c#
services.RegisterAuthPermissions<Example4Permissions>()
    .UsingEfCoreSqlServer(Configuration.GetConnectionString("DefaultConnection")) 
    .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
    .IndividualAccountsAddSuperUser()
    .SetupAspNetCorePart();
```

## A list of the AuthP's extensions and what you can use

The AuthP library has lots of options and the table below shows the various groups and when and where they should go.

| Group | Method |  Needed? |
| --- | --- | --- |
| Configure | `RegisterAuthPermissions<TEnum>` | Required, first. |
| Database | `UsingEfCoreSqlServer`<br>`UsingInMemoryDatabase` | One required |
| Bulk Load | `AddTenantsIfEmpty`<br>`AddRolesPermissionsIfEmpty`<br>`AddAuthUsersIfEmpty`<br>`RegisterFindUserInfoService<TLookup>` | Optional |
| User Admin | `RegisterAuthenticationProviderReader<TSync>` | Required for admin |
| SuperUser | `AddSuperUserToIndividualAccounts` | Optional |
| Finalize | `SetupAspNetCorePart`<br>`SetupAspNetCoreAndDatabase`<br>`SetupForUnitTestingAsync` |  One required, last |

The following sections explains each Group in more detail.

### Configure

You must start with the `RegisterAuthPermissions<TEnum>` method. This starts the registration of AuthP. There are two parts to this configuration method.

1. It allows you to tell AuthP what enum contains your Permissions.  
_NOTE: This method will throw a exception you Permissions the enum has the `: ushort` applied to it._
2. You can set the AuthP's options. At the moment there are
    1. Setting up AuthP's JWT Token, and optional JWT refresh value (see [Setting up a JWT Token](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/setup/JWTToken.md) for more details).
    2. Setting up AuthP's multi-tenant feature (see [Setting up multi-tenant](!!!!) for the options).

### Database

You need to select a database type where AuthP can store its information. At the moment there are only two options:

#### 1. `UsingEfCoreSqlServer`

This method takes in a connection string to a SQL Server database where the AuthP entities are stored.  

_NOTE: The AuthP code is designed to be part of the database that the application to reduce the number databases your application needs. It has its own EF Core migration table so that it can be migrated without effecting other parts of the database._

#### 2. `UsingInMemoryDatabase`

This creates a SQLite in-memory database. On startup the database will be created, and when the application closed the database will be disposed.

An in-memory database is there mainly for unit testing your application. The Bulk load methods are also useful for seeding the database at the start of a unit test.

### Bulk Load

The bulk load methods allow you to setup Role, Multi-tenants, and AuthUsers, but <ins>the database has no Roles,  Multi-tenants or AuthUsers</ins> respectively. This provides a quick way to load AuthP settings on startup. That's mainly when unit testing, but it could be helpful when adding AuthP to an existing application.

The Bulk Load features are also available as an admin service for cases where you want to load a large number of Roles,  Multi-tenants or AuthUsers - see [Bulk Load services](!!!!) or the comments in the code for format of the data needed for Bulk Loading.

### User Admin

The authentication provider's users are the master list of users and the AuthP's AuthUsers need to be in synced to authentication provider's users. The `RegisterAuthenticationProviderReader<TSync>` extension method allows you to provide a service (that implements the `ISyncAuthenticationUsers` interface ) that AuthP can use to 'sync' its AuthUsers with the authentication provider's users. See [AuthUser's admin sync service](!!!!) for more details on how this works.

### SuperUser

If you are using the Individual Accounts authentication provider, then you create a new database it will be empty of any users. That can be a problem as you can't access the admin features to add more users. The `AddSuperUserToIndividualAccounts` will  create a new user in the Individual Accounts database on startup using information in your appsettings.json file shown below:

```json
  "SuperAdmin":
  {
    "Email": "Super@g1.com",
    "Password": "Super@g1.com"
  }
```

This, combined with the Bulk Load extension methods you can create a AuthUser that has the `AccessAll` Permission member (see) [Setting up your Permissions - access](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/setup/permissions.md#creating-the-permissions-enum)). This then allows you to use this SuperUser to set up normal admin users.

_NOTE: There is a small security issue in that if someone changes the SuperUser settings in the appsettings.json file, then you would get an extra SuperUser when the application is next deployed.


### Finalize

 `SetupAspNetCorePart`<br>`SetupAspNetCoreAndDatabase`<br>`SetupForUnitTestingAsync` 

 ????????????????????????????????????????????????????????
 ????????????????????????????????????????????????????????
 ????????????????????????????????????????????????????????

## Additional resources

[Setting up JWT Tokens using AuthP](!!!!)
[Bulk Load services](!!!!)
[AuthUser's admin service](!!!!)