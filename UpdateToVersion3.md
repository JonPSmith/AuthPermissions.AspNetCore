# Migrating AuthPermissions.AspNetCore 2.* to 3.0

Version 3 of the AuthPermissions.AspNetCore library (shortened to **AuthP** from now on) contains a new sharding feature for multi-tenant applications. Please read the article called [Part6: Using sharding to build multi-tenant apps using EF Core and ASP.NET Core] LINK NEEDED for a detailed explanation of sharding and how AuthP library provides a sharding implementation.

This article explains how to update an existing AuthPermissions.AspNetCore 2.* project to AuthPermissions.AspNetCore 3.0. I assume that you are using Visual Studio.

## TABLE OF CONTENT

These are things you need to do to update aan application using AuthP version 2.0 to

- **BRAKING CHANGE**: [Update your ITenantChangeService code]
- **Automatically applied**: 
  The AuthP DbContext requires a migration to add some new properties in the `AuthUser` and `Tenant` classes. This is a non-breaking migration and will be automatically applied to the AuthP database on startup.

## BRAKING CHANGE: Update your `ITenantChangeService` code

There is a significant change to the code your need to write to link the AuthP's tenant commands - create, update, delete and Move (hierarchical). The changes are to support the new Sharding feature and reduce the linking between databases, but it also makes it possible to store the the tenant data in different database from the database uses for admin data. See issue #15 for more detail on why this update was done.

Here are the main changes:

### 1. Remove `GetNewInstanceOfAppContext` method and use dependency injection (DI)

The new approach to obtaining a instance of your application's DbContext is to use DI injection via the class's constructor. This removes the need for the  `GetNewInstanceOfAppContext` method. See the code below from Example3 shows how to inject the application's DbContext using DI - NOTE: The logger is optional - I use it to log any problems in my code.

```c#
public class InvoiceTenantChangeService : ITenantChangeService
{
    private readonly InvoicesDbContext _context;
    private readonly ILogger _logger;

    public InvoiceTenantChangeService(
        InvoicesDbContext context, 
        ILogger<InvoiceTenantChangeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    //The rest of the code is left out
```

This change makes your tenant change service more normal and removes the duplication of database setup code that was in the `GetNewInstanceOfAppContext` method.

### 2. The `AppConnectionString` in the AuthP options is no longer needed

With the change in version 3 to injecting the tenant DbContext you don't have to provide the connection string for your tenant DbContext.

### 3. Changes to the names and parameters of the methods

Every method in the [`ITenantChangeService`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions/AdminCode/ITenantChangeService.cs) has changed and some new methods have been added. If you want to update one of your tenant change service I recommend you look at updated tenant change in the main branch, which have been updated to the version 3 design

- For a single-level multi-tenant have look at Example3s [InvoiceTenantChangeService](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example3.InvoiceCode/EfCoreCode/InvoiceTenantChangeService.cs).
- For a hierarchical multi-tenant have look at Example4, [RetailTenantChangeService](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.ShopCode/EfCoreCode/RetailTenantChangeService.cs).
- For the single-level + sharding multi-tenant version look at ???

### 4. Use a transaction in methods that has multiple updates

Previously the AuthP's tenant admin service created a transaction that covered both the update to the AuthP's DbContext and your application's DbContext. In the new design you have to add a transaction **if you have multiple, separate updates** to the database. This stops the possibility a partial update where of some updates have been applied, but an error happens during later updates. That could mean some data was lost.

For instance, in Example3's tenant change service it uses a transaction in the Delete code because that code uses multiple  `ExecuteSqlRawAsync` and a call to `SaveChangesAsync` - see the `SingleTenantDeleteAsync` in the [InvoiceTenantChangeService](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example3.InvoiceCode/EfCoreCode/InvoiceTenantChangeService.cs) class for an example of that.

In a hierarchical multi-tenant application you will now have multiple tenants to delete or move, and all those changes should be done within one transaction - see the [RetailTenantChangeService](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.ShopCode/EfCoreCode/RetailTenantChangeService.cs) example for how this is done.

END