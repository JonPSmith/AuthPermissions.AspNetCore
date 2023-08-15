# AuthPermissions.AspNetCore

The AuthPermissions.AspNetCore library (shortened to AuthP) provides extra authorization features to a ASP.NET Core application. Here are AuthP's three main features:

- An improved Role authorization system where the features a Role can access can be changed by an admin user (i.e. no need to edit and redeploy your application when a Role changes).
- Provides features to create a multi-tenant database system, either using one-level tenant or multi-level tenant (hierarchical).
- Implements a JWT refresh token feature to improve the security of using JWT Token in your application.

The AuthP is an open-source library under the MIT licence (and remain as a open-source library for ever) and the NuGet package can be [found here](https://www.nuget.org/packages/AuthPermissions.AspNetCore/). **The documentation can be found in the [GitHub wiki](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki)** and see [ReleaseNotes](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/ReleaseNotes.md) for details of changes. There is also a [roadmap discussion](https://github.com/JonPSmith/AuthPermissions.AspNetCore/discussions/2) containing the plans for this library.

The AuthP library is being built in versions (see [roadmap](https://github.com/JonPSmith/AuthPermissions.AspNetCore/discussions/2)). If you have already built your application using an older version, then you need to look at the following "how up update" documents

- From Migrating from AuthPermissions.AspNetCore 3, 4, 5 to 6.0 see [UpdateToVersion6.md](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion5.md)
- From Migrating from AuthPermissions.AspNetCore 2, 3 or 4.* to 5.0 see [UpdateToVersion5.md](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion5.md)
- From Migrating from AuthPermissions.AspNetCore 2.* to 3.0 see [UpdateToVersion3.md](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion3.md)
- From Migrating from AuthPermissions.AspNetCore 1.* to 1.0 see [UpdateToVersion2.md](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion2.md)

The AuthP library also:

- Works with any ASP.NET Core authentication provider. Currently has built-in individual accounts and Azure Active Directory versions, but you can create your own.
- Works with either Cookie authentication or JWT Token authentication.
- Contains an admin services to sync the authentication provider users with  AuthP's users.
- Has a comprehensive set of admin services to manage AuthP's Roles, Tenants and Users.


## Example code in this repo

The AuthPermissions.AspNetCore repo contains the following example of using AuthP with ASP.NET Core applications listed below. All of them can be run and show a HOME page describes what the application does (apart from the WebAPI example, which shows the Swagger display).

### Example1 - Roles and permissions

This is a ASP.NET Core Razor Pages application using the Individual Accounts authentication provider with Cookie authentication. Look at this example for:

- A very simple example of using AuthP's authorization Roles and AuthUsers
- A comparision between ASP.NET Core authorization with AuthP's authorization
- A basic admin of Auth Users.

### Example2 - JWT Token in ASP.NET Core Web API

This is a ASP.NET Core WebAPI application using the Individual Accounts authentication provider with JWT Token authentication. Look at this example for:

- An example of using AuthP to create a JWT Token for you.
- An example of using AuthP's JWT refresh feature.

See the video [Improving JWT Token Security](https://www.youtube.com/watch?v=DtfNUHgwKyU) for more about this feature works.

_NOTE: When running this example and you want to login you must run one of the authentication login WebAPIs and then copy the just the JWT Token string in into Swagger's Authorize box. Also, the default lifetime of the JWT Token is 5 minutes, so you wll get logged out quickly (this is done to check the AuthP's JWT refresh feature)._

### Example3 - Single level multi-tenant application

This is a ASP.NET Core MVC application using the Individual Accounts authentication provider with Cookie authentication. Look at this example for:

- How to use AuthP to create a single-level multi-tenant system.
- Demo of changing the look and feel of an app when a tenant logs in.

### Example4 - Hierarchical multi-tenant application

This is a ASP.NET Core MVC application using the Individual Accounts authentication provider with Cookie authentication. Look at this example for:

- how to use AuthP to create a hierarchical multi-tenant system.
- A more substantial application with lots of Permissions, Roles, Tenants and Users.
- How the AuthP' admin code can be used to control Roles, Users and Tenants.

### Example5 - Login via Azure AD

This is a ASP.NET Core MVC application using the Azure AD authentication provider with Cookie authentication. Look at this example for:

- How to use Azure AD authentication with the AuthP library.


## Notes on creating a NuGet package

The AuthPermissions.AspNetCore library contains more than one project. For this reason you can't (currently) create a NuGet package using NuGet values in a .csproj file.

For this reason I use the `JonPSmith.MultiProjPack` dotnet tool to create the NuGet package using the following command in a command line on the AuthPermissions.AspNetCore directory.

```
> MultiProjPack R
```

_NOTE: If you don't want to use the `JonPSmith.MultiProjPack` dotnet tool you should find a `CreateNuGetRelease.nuspec` file which you can call with the following command

```
> dotnet pack -p:NuspecFile=CreateNuGetRelease.nuspec -v q -o ./nupkg
```
