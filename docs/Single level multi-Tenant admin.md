The AuthP library contains a IAuthTenantAdminService service that contains various admin features for managing multi-tenant systems. This document describes the single level multi-tenant admin services and give you some examples of how they might be used in an application.

The [Example3 application](https://github.com/JonPSmith/AuthPermissions.AspNetCore/tree/main/Example3.MvcWebApp.IndividualAccounts) provides an example single level multi-tenant application containing code to manage invoices. You can clone the https://github.com/JonPSmith/AuthPermissions.AspNetCore/ and run this application to see how it works.

You must log in as 'AppAdmin@g1.com' or 'Super@g1.com' to access all the admin features, and other users (e.g. user1@some-company.com) to work within a single multi-tenant set of data.

## Explaining the single level multi-tenant features

Here is a list of the various methods used to, with examples from Example3 application in the repo. These use methods in the [IAuthTenantAdminService](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions/AdminCode/IAuthTenantAdminService.cs) service. _NOTE: The [IAuthTenantAdminService](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions/AdminCode/IAuthTenantAdminService.cs) contains comments on each method._

### Creating a new Tenant

Say a new company wants to use your application, then you would create a new `Tenant` to get the `DataKey` for filtering your application's data. To create a single level tenant you use the tenant admin `AddSingleTenantAsync` method (see code below). The tenant name must be unique. If it isn't the method returns an error in its status.

```c#
var status = await _authTenantAdmin.AddSingleTenantAsync(input.TenantName);
```

A call to the `AddSingleTenantAsync` method will also call the `CreateNewTenantAsync` method in the your implementation of the `ITenantChangeService` interface (see [[Multi tenant configuration]] for more about `ITenantChangeService`). This allows your own application data to create a local tenant entity with the tenant name, which can be useful if you want to show the tenant name in your app.

### Adding / changing a Tenant to an AuthP user

