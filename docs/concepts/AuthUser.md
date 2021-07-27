# Explaining AuthP's AuthUser

If you create an ASP.NET Core application that people have to log into ASP.NET Core you will use a ASP.NET Core _authentication provider_ (see [ASP.NET Core docs on authentication](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)). The authentication provider's job is to check that the user that is logging in is valid, e.g. it is known to the authentication provider and they provided the correct secret information. Some authentication providers, like ASP.NET Core’s Individual Accounts authentication provider, stores the user's information in a database linked to your application, but many authentication provider, such as Azure active directory or Google, store the information externally.

But for the AuthP library we need extra data not available from the authentication provider - this is where AuthP's `AuthUser` comes in.

## AuthP's `AuthUser` entity

AuthP's `AuthUser` entity is linked logged in user via the authentication provider _user id_, which is a string. This `AuthUser` entity holds the [AuthP's Roles](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/concepts/roles.md) and the optional [AuthP's multi-tenant option](!!!!) information.

Of course the authentication provider is in charge of who are valid users, and the AuthP's [AuthUser's admin service](!!!!) has a [sync](!!!!) feature which will tell you if and of the authentication provider's have changes and provides a way to update the AuthP's AuthUsers. In addition the [AuthUser's admin service](!!!!) allows you alter an AuthUser's AuthP's Roles and multi-tenant information.

## How are the AuthP claims added to the logging-in user?

When a user logs in the AuthP will automatically add extra claims if a Cookie Authentication is configured, or if you are using JWT Token Authentication you call a method to build the JWT Token which includes the AuthP's claims.

Then on every HTTP request ASP.NET Core will automatically all the claims from the Cookie Authentication or JWT Token and builds a `ClaimsPrincipal`, which is in the HTTP context under the property `User`. The diagram below shows this in action.

![Extract Permission Claims](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/images/ExtractPermissionsClaim.png)

## Additional resources

- [Using Permissions in your application](!!!!)
- [Using JWT Token Authentication](!!!!)
- [Admin -> AuthUsers](!!!!)