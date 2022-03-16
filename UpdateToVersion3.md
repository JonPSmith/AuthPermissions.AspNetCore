# Migrating AuthPermissions.AspNetCore 2.* to 3.0

Version 3 of the AuthPermissions.AspNetCore library (shortened to **AuthP** from now on) contains a new sharding feature for multi-tenant applications. Please read the article called Using sharding to build multi-tenant apps using EF Core and ASP.NET Core] for a detailed explanation of sharding and how AuthP library provides a sharding implementation.

This article explains how to update an existing AuthPermissions.AspNetCore 2.* project to AuthPermissions.AspNetCore 3.0. I assume that you are using Visual Studio.

## TABLE OF CONTENT

These are things you need to do to update aan application using AuthP version 2.0 to

- **BRAKING CHANGES**:
  - [Update your ITenantChangeService code]



## BRAKING CHANGE: Update your ITenantChangeService code

There is a significant change to the code your need to write to link the AuthP's tenant commands - create, update, delete and Move (hierarchal). Overall the code is simpler, but it does mean you need to update your code to handle your part of these commands in your application's DbContext. Here are the changes you need to change from version 2 to version 3 of the `ITenantChangeService`. See issue #15 for why this update was done.

_NOTE: See  Example3, [InvoiceTenantChangeService](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example3.InvoiceCode/EfCoreCode/InvoiceTenantChangeService.cs), and Example4, [RetailTenantChangeService](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.ShopCode/EfCoreCode/RetailTenantChangeService.cs), for examples of tenant change code for a single-level and hierarchical multi-tenant respectively._

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

### 2. Use a transaction in methods that has multiple updates

Previously the AuthP's tenant admin service created a transaction that covered both the update to the AuthP's DbContext and your application's DbContext. In the new design you have to add a transaction **if you have multiple, separate updates** to the database. This stops the possibility a partial update where of some updates have been applied, but an error happens during later updates. That could mean some data was lost.

For instance, in Example3's tenant change service it uses a transaction in the Delete code because that code uses multiple  `ExecuteSqlRawAsync` and a call to `SaveChangesAsync` - see the `SingleTenantDeleteAsync` in the [InvoiceTenantChangeService](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example3.InvoiceCode/EfCoreCode/InvoiceTenantChangeService.cs) class.

### 3. Changes to the names and parameters of the methods

The changes are:

- In all the methods the `appTransactionContext` is removed. In version 3 you use DI to get an instance of the your application's DbContext.
- The `HandleTenantDeleteAsync` has been split into
  - `SingleTenantDeleteAsync` is called for a delete of a single-level tenant.
  - `HierarchicalTenantDeleteAsync`is called for a delete of a hierarchical tenant.
- The new `HierarchicalTenantDeleteAsync` method takes in a list of tenants to be deleted (previously the method was called multiple times, but in version 3 you need to apply all the deletes within a transaction so that if a error happens then all the updates will be rolled back).
