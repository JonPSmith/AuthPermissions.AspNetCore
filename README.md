# AuthPermissions.AspNetCore

The AuthPermissions.AspNetCore library (shortened to AuthP) provides extra authorization features to a ASP.NET Core application. Here are AuthP's three main features:

- An improved Role authorization system where the features a Role can access can be changed by an admin user (i.e. no need to edit and redeploy your application when a Role changes).
- Implements a JWT refresh token feature to improve the security of using JWT Token in your application.
- Provides features to create a multi-tenant database system, either using one-level tenant or multi-level tenant (hierarchical).

The AuthP library also:

- Works with any ASP.NET Core authentication provider.
- Works with either Cookie authentication or JWT Token authentication.
- Contains an admin services to sync the authentication provider users with  AuthP's users.
- Has a comprehensive set of admin services to manage AuthP's Roles, Tenants and Users.

The AuthP is an open-source library under the MIT licence. The documentation can be found in the [GitHub wiki](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki) and see [ReleaseNotes](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/ReleaseNotes.md) for details of changes.

## Example code in this repo

The AuthPermissions.AspNetCore repo contains the following example of using AuthP with ASP.NET Core applications listed below. All of them can be run and show a HOME page describes what the application does (apart from the WebAPI example, which shows the Swagger display).

### Example1.RazorPages.IndividualAccounts

This is a ASP.NET Core Razor Pages application using the Individual Accounts authentication provider with Cookie authentication. Look at this example for:

- A very simple example of using AuthP's authorization Roles and AuthUsers
- A comparision between ASP.NET Core authorization with AuthP's authorization

### Example2.WebApiWithToken.IndividualAccounts

This is a ASP.NET Core WebAPI application using the Individual Accounts authentication provider with JWT Token authentication. Look at this example for:

- An example of using AuthP to create a JWT Token for you.
- An example of using AuthP's JWT refresh feature.

_NOTE: When running this example and you want to login you must run one of the authentication login WebAPIs and then copy the just the JWT Token string in into Swagger's Authorize box. Also, the default lifetime of the JWT Token is 5 minutes, so you wll get logged out quickly (this is done to check the AuthP's JWT refresh feature)._

### Example3 - not built yet

This example shows how AuthP would work with [Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-whatis) as the authentication provider.

### Example4.MvcWebApp.IndividualAccounts

This is a ASP.NET Core MVC application using the Individual Accounts authentication provider with Cookie authentication.
Look at this example for:

- A more substantial application with lots of Permissions, Roles, Tenants and Users.
- how to use AuthP to create a hierarchical multi-tenant system.
- How the AuthP' admin code can be used to control Roles, Users and Tenants.


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
