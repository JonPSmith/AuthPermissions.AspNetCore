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
_NOTE: This method will throw an exception you Permissions the enum has the `: ushort` applied to it._
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

The bulk load methods allow you to setup Role, Multi-tenants, and AuthUsers, but only if <ins>the database has no Roles,  Multi-tenants or AuthUsers</ins> respectively. This provides a quick way to load AuthP settings on startup. That's mainly when unit testing, but it is be helpful when adding AuthP to an existing application.

_The Bulk Load features are also available as an admin service for cases where an admin user wants to load a large number of Roles,  Multi-tenants or AuthUsers._

There are three Bulk Load extension methods which will update the AuthP's database on application startup. They are:

- `AddTenantsIfEmpty`: Adds AuthP multi-tenant `Tenants` if no `Tenants` already exists.
- `AddRolesPermissionsIfEmpty` : Adds AuthP `Roles` if no `Roles` already exists.
- `AddAuthUsersIfEmpty`: Adds AuthP's Users if no `AuthUsers` already exist.

The other method is the `RegisterFindUserInfoService<TLookup>`, which allows you to provide class which implements the `IFindUserInfoService` interface. Your class is registered to the service provider and is used by the Bulk Load (setup or admin services) to obtain the UserId of a authentication provider user via either the user's Email address, or their UserName.

See the [Bulk Load admin services](!!!!) documentation for information on format of the data needed for Bulk Loading.

### User Admin

The authentication provider's users are the master list of users and the AuthP's AuthUsers need to be in synced to authentication provider's users. The `RegisterAuthenticationProviderReader<TSync>` extension method allows you to provide a service (that implements the `ISyncAuthenticationUsers` interface ) that AuthP can use to 'sync' its AuthUsers with the authentication provider's users. See [AuthUser's admin sync service](!!!!) for more details on how this works.

## SuperUser

If you are using the Individual Accounts authentication provider, then when the Individual Accounts is created it will be empty of any users. That can be a problem when you have deployed the application to a new server, as no one is registered  to access the admin features to add more users. The `AddSuperUserToIndividualAccounts` will  create a new user in the Individual Accounts database on startup using information in your appsettings.json file shown below:

```json
  "SuperAdmin":
  {
    "Email": "Super@g1.com",
    "Password": "Super@g1.com"
  }
```

This, combined with the Bulk Load extension methods you can create a AuthUser that has the `AccessAll` Permission member (see[Setting up your Permissions - access](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/setup/permissions.md#creating-the-permissions-enum) for more info). This then allows you to use this SuperUser to set up normal admin users.

_NOTE: There is a small security issue in that if someone changes the SuperUser settings in the appsettings.json file, then you would get an extra SuperUser when the application is next deployed._


## Finalize

The final extension method in the AuthP setup register all the services and, optionally, run some migration/seeding on startup of the ASP.NET Core. There are three options to chose from:

### 1. `SetupAspNetCorePart` extension method

This assumes that the AuthP's database has already been created/migrated. This means it will work for ASP.NET Core deployments that runs multiple instances of the application (known as _scale out_ in Azure).

If you have included Bulk Load extension methods in your configuration you must set the `addRolesUsersOnStartup` parameter in the `SetupAspNetCorePart` method to `true` (defaults to `false`) for get Bulk Load data added to the AuthP's database. _NOTE: This will not work if the deployment runs multiple instances of the application._

### 2. `SetupAspNetCoreAndDatabase` extension method

This method will apply a migration to the AuthP database on startup, and then seeds the AuthP database with any Bulk Load (if provided).

_NOTE: This will not work if the deployment multiple instances of the application._

### 3. `SetupForUnitTestingAsync` extension method

If you want to unit test (also known as integration test) your application with the AuthP features you should use the `SetupForUnitTestingAsync` extension method. This returns the built `ServiceProvider`, which contains all the AuthP services.

The [Bulk Load methods](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/setup/startup.md#bulk-load) are useful when unit testing as you can quickly set up the AuthP parts of the application. To make this work the `SetupForUnitTestingAsync` extension method will do the following steps before it returns:

1. Build the `ServiceProvider` which contains all the AuthP's services (which is returned).
2. Then it creates the AuthP database using EF Core's `EnsureCreatedAsync` method.
3. And finally it applied any Bulk Load methods to seed the AuthP's database.

See the [Unit Testing](!!!!) page for more on how to unit test applications that use AuthP.


## Additional resources

- [Setting up JWT Tokens using AuthP](!!!!)
- [Bulk Load services](!!!!)
- [AuthUser's admin services](!!!!)
- [Unit testing applications using AuthP](!!!!)