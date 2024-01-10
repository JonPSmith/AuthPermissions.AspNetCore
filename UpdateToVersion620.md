# Upgrade to the SignInAndCreateTenant service - version 6.2.0

AuthP version 6.2.0 has a **BREAKING CHANGE**, but **ONLY** if you are using the "Sign up for a new tenant, with versioning" (shortened to "Sign up Tenant") feature **AND** your multi-tenant uses [sharding](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki/Sharding-explained). Otherwise you can ignore this breaking change.

The information on updated "Sign up Tenant" feature can be found at:

- [Sign up for a new tenant, with versioning](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki/Sign-up-for-a-new-tenant%2C-with-versioning) page in the documentation.
- [Multi-tenant apps with different versions can increase your profits](https://www.thereformedprogrammer.net/multi-tenant-apps-with-different-versions-can-increase-your-profits/) article in my tech blog.

## What changes do I you need to do my existing app?

The the `ISignInAndCreateTenant` hasn't changed, but the `IGetDatabaseForNewTenant` service, which you needed to create if your multi-tenant app is using sharding, is replaced it with the  `ISignUpGetShardingEntry` service. If you used `IGetDatabaseForNewTenant`, then you will get a compile error and you need to remove your old `ISignUpGetShardingEntry` service with and create a new service that follows the `IGetDatabaseForNewTenant` interface.

There are two demo versions in the code before release of version 6.2.0 and they have been updated to the new version. See the updated demos below:

- For hybrid sharding see the [`DemoGetDatabaseForNewTenant`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions.SupportCode/DemoGetDatabaseForNewTenant.cs) for the changes.
- For sharding-only see the [`DemoShardOnlyGetDatabaseForNewTenant`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions.SupportCode/DemoShardOnlyGetDatabaseForNewTenant.cs) for the changes.

## Why I changed the SignInAndCreateTenant service?

The original version of the `SignInAndCreateTenant` service will try to “undo” the sign up for a tenant if there is an error. The original would try to delete the tenant so that the new user can try again. Errors are rare in a properly build application, but  problem is sometimes the "undo"  doesn't work, which can stop the new user from using your multi-tenant application.

In the 6.2.0 version of the AuthP library the `SignInAndCreateTenant` service works works in a different way: instead of trying to “undo” things it tells the user to ask your support admin to look at the problem, with a unique string to help the support admin to look what went wrong.

The main change is that is uses a unique name (formed from current time) the for the tenant name, tenant database name, etc. and only when the sign up has successfully finished that the correct tenant name.

Other improvements are:

- The original “undo” code was very complex, which makes it hard to cover / test every situations. The new code is much simpler because the unique name makes each "Sign Up" to be different so the new user can again, or talk to your support admin with a reference of what they were trying to do. 
- The original code didn't report `Exception`s properly. The new code logs the Exception with the unique name and send the user a message saying they should contact your support team, with the unique name. This allows your support team to find the Exception log and manually set up the new user.

END