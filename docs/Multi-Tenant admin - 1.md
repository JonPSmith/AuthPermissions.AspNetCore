The AuthP library contains a IAuthTenantAdminService service that contains various admin features for managing multi-tenant systems. This document describes the multi-tenant admin services and give you some examples of how they might be used in an application.

There is two example implementation of the multi-tenant admin features:

- Example3's [TenantController](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example3.MvcWebApp.IndividualAccounts/Controllers/TenantController.cs), which has a single level multi-tenant application containing code to manage invoices. You must log in as 'AppAdmin@g1.com' or 'Super@g1.com' to access all the admin features, and other users (e.g. user1@some-company.com) to work within a single multi-tenant set of data.

- Example4's [TenantController](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.MvcWebApp.IndividualAccounts/Controllers/TenantController.cs), which has a hierarchical multi-tenant application to manage the stoke / sales in shops. These shops are 'grouped', so that higher manager can see the state of all the shops in their group. You must log in as 'AppAdmin@g1.com' or 'Super@g1.com' to access all the admin features, or the shop users (e.g. Tie4UManager@4uInc.com) to access the stock / sales data for that shop.

## Understanding AuthP's `Tenant` class does

The [`Tenant` class](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions/DataLayer/Classes/Tenant.cs) contains information that defines the multi-tenant data. Once created it contains the method `GetTenantDataKey()` which return a unique string that can be used to filter the data - see [[Multi-tenant-explained]]. It also name of the multi-tenant area, e.g. "Company XYZ".

If an AuthP user has a `Tenant` class linked to it, then the user will have access to the data which has the same the DataKey as the of the  `Tenant`'s DataKey.

The other important thing is the multi-tenant can be either have a single level, or a hierarchical has multiple levels (see [[Multi tenant explained]] for more details) -  you define which type of multi-tenant type you want during the registering / configuring the AuthP library on startup (see [[Multi tenant configuration]] for more details).

## Explaining the multi-tenant features

Here is a list of the various methods used to , with example pages.