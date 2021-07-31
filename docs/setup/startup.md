# Setting up the AuthPermissions in ASP.NET Core

Like all ASP.NET Core libraries the AuthP library has to be registered as a service. Also, yhe AuthP library follows the ASP.NET Core of format of configuring itself with extension methods, and providing configuration points.

Here is a simple example of what registering the AuthP library would look like, but there are lots of different extension methods and configurations.

```c#
services.RegisterAuthPermissions<Example4Permissions>(options =>
    {
        options.MigrateAuthPermissionsDbOnStartup = true;
    })
    .UsingEfCoreSqlServer(Configuration.GetConnectionString("DefaultConnection")) 
    .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
    .IndividualAccountsAddSuperUser()
    .FinalizeAuthPermissions();
```

## A list of the AuthP's extensions and what you can use

The AuthP library has lots of options and the table below shows the various groups and when and where they should go.

| What | Method |  Needed? |
| --- | --- | --- |
| Main config | `RegisterAuthPermissions<TEnum>` | Required, 1st. |
| Database | `UsingEfCoreSqlServer`<br>`UsingInMemoryDatabase` | One required |
| User Admin | `RegisterAuthenticationProviderReader<TSync>` | Required for admin |
| SuperUser | `AddSuperUserToIndividualAccounts` | Optional |
| Bulk Load | `AddTenantsIfEmpty`<br>`AddRolesPermissionsIfEmpty`<br>`AddAuthUsersIfEmpty`<br>`RegisterFindUserInfoService<TLookup>` | Optional |
| Finalize | `SetupAspNetCorePart`<br>`SetupAspNetCoreAndDatabase`<br>`SetupForUnitTestingAsync` |  One required, last |



## Additional resources

[Setting up JWT Tokens using AuthP](!!!!)