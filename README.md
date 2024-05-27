# AuthPermissions.AspNetCore

The AuthPermissions.AspNetCore library (shortened to AuthP) provides extra authorization features to a ASP.NET Core application. Here are AuthP's three main features:

- An improved Role authorization system where the features a Role can access can be changed by an admin user (i.e. no need to edit and redeploy your application when a Role changes).
- Provides features to create a multi-tenant database system, either using one-level tenant or multi-level tenant (hierarchical).
- Implements a JWT refresh token feature to improve the security of using JWT Token in your application.

The AuthP is an open-source library under the MIT licence (and remain as a open-source library for ever) and the NuGet package can be [found here](https://www.nuget.org/packages/AuthPermissions.AspNetCore/). **The documentation can be found in the [GitHub wiki](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki)** and the AuthP [roadmap](https://github.com/JonPSmith/AuthPermissions.AspNetCore/discussions/2) defines the different versions of this library. 

## List of versions and which .NET framework they support

- Version 8.?.? supports NET 8 only (simpler to update to next NET release)
- Version 6.?.? supports NET 6, 7 and 8
- Version 5.?.? supports NET 6 and 7

If you have already built your application using an older version, then you need to look at the following "how up update" documents

- From Migrating from AuthPermissions.AspNetCore 3, 4, 5 to 6.1 see [UpdateToVersion620.md](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion620.md). NOTE: you only need to do this if you are using the "Sign up for a new tenant, with versioning" (shortened to "Sign up Tenant") feature **AND** your multi-tenant uses [sharding](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki/Sharding-explained).
- From Migrating from AuthPermissions.AspNetCore 3, 4, 5 to 6.0 see [UpdateToVersion6.md](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion5.md)
- From Migrating from AuthPermissions.AspNetCore 2, 3 or 4.* to 5.0 see [UpdateToVersion5.md](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion5.md)
- From Migrating from AuthPermissions.AspNetCore 2.* to 3.0 see [UpdateToVersion3.md](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion3.md)
- From Migrating from AuthPermissions.AspNetCore 1.* to 1.0 see [UpdateToVersion2.md](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/UpdateToVersion2.md)

The AuthP library also:

- Works with any ASP.NET Core authentication provider. Currently has built-in individual accounts and Azure Active Directory versions, but you can create your own.
- Works with either Cookie authentication or JWT Token authentication.
- Contains an admin services to sync the authentication provider users with  AuthP's users.
- Has a comprehensive set of admin services to manage AuthP's Roles, Tenants and Users.


## How to create an AuthPermissions.AspNetCore NuGet package

The AuthPermissions.AspNetCore library contains more than one project. For this reason you can't (currently) create a NuGet package using NuGet values in a .csproj file. For this reason I created a `JonPSmith.MultiProjPack` dotnet tool to create the NuGet package using the following command in a command line on the AuthPermissions.AspNetCore directory. 

_See [`JonPSmith.MultiProjPack` GitHub](https://github.com/JonPSmith/MultiProgPackTool) for why I created the `JonPSmith.MultiProjPack` and more about its features._

### 1. Install the MultiProjPack dotnet tool

On your computer you need to install the global tool using the command below (see [this documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) to learn about global tools).

`dotnet tool install JonPSmith.MultiProjPack --global`

_NOTE: To update the MultiProjPack .NET tool you need to run the command `dotnet tool update JonPSmith.MultiProjPack --global`. Or to uninstall this tool you should use `dotnet tool uninstall JonPSmith.MultiProjPack --global` command._

### 2. Compile the AuthPermissions.AspNetCore in release Mode

You must select "Release" compile mode and then use the "Build > Rebuild Solution" to ensure a new release version of the AuthPermissions.AspNetCore is available.

### 3. Run the `MultiProjPack` tool to create the 

You run the `MultiProjPack` tool from a command line in the `AuthPermissions.AspNetCore` directory. I use Visual Studio's "Open in Terminal" using the command below ("R" is short for "Release")

```console
MultiProjPack R
```

*NOTE: If you want to create a new version of the NuGet package you must update the NuGet `version` and most likely the `releaseNotes` in the `MultiProjPack.xml` before you call the `MultiProjPack` tool.*

The created .nupkg file will be found in the `AuthPermissions.AspNetCore.nupkg` directory AND in the user's `{USERPROFILE}\LocalNuGet` directory.
