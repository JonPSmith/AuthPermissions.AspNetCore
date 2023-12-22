# Upgrade to the SignInAndCreateTenant service - version 6.2.0

AuthP version 6.2.0 has a **BREAKING CHANGE** around the `SignInAndCreateTenant` ???????????????????????

## Why I changed the SignInAndCreateTenant service

The AuthP library has a service called `SignInAndCreateTenant` which a new user can log in and obtain a new tenant (see the [“Multi-tenant apps with different versions can increase your profits”](https://www.thereformedprogrammer.net/multi-tenant-apps-with-different-versions-can-increase-your-profits/) article for more on this). The current (6.1.0) AuthP version of the `SignInAndCreateTenant` service will try to “undo” the sign up for a tenant so that the user can again but has the following issues.

- You can’t always undo a new tenant, which means the new user can’t use the multi-tenant app.
- When an Exception occurs its not logged, which makes it to  
- The “undo” feature code very complex and uses direct access to the tenant instead of using the AuthTenantAdminService service.

Therefore I have created a new `SignInAndCreateTenant` service which uses a very different approach to overcome the issues in the 6.1.0 AuthP version. The changes are:

- If a fatal error, e.g Exception, occurs:
  1. They are logged within the application, with a unique name.
  2. The user is given the error unique name and asked to contact the App's support team.
  3. A new service is added that allows the support team to create a tenant for a tenant using the version data.

The new design won't create a tenant that would stop the user creating the tenant they want because the service uses a temporary name, e.g. "TempSignIn-{tenantId}-{DateTime}" for the tenant and any sharding entry name during setting up the new tenant. Once the process has successfully ended it changes the tenant's name(s) to the name that the user provided. This has two benefits:

- This makes the `SignInAndCreateTenant`'s code much simpler (the original code used direct Auth database accesses rather than the normal services such as `IAuthTenantAdminService` and `IGetSetShardingEntries`).
- If an fatal error occurs, then the admin user can look for "TempSignIn...", which might help the admin user to see what went wrong.



