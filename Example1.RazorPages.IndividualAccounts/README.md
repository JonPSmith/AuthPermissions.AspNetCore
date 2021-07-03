# Example1.RazorPages.IndividualAccounts

This project contains a example of using the AuthPermissions.AspNetCore library in ASP.NET Core razor page web app with user data provided by the individual accounts approach. This is one of the simplest approaches using:

- **Application**: ASP.NET Core, Razor Pages
- **AuthorizationProvider**: ASP.NET Core's individual accounts
- **CookieOrToken**: Cookie
- **DataKey**: not used
- **Databases**: Two databases
  - Individual accounts: InMemoryDatabase:
  - AuthPermissions: In-memory database (uses SQLite in-memory).

The ASP.NET Core code comes comes from the [ASP.NET Core documentation on building razor page web app individual accounts authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/secure-data), but the handling of the visibilty of the contact manager features are handled by the AuthPermissions.AspNetCore library.

The AuthPermissions.AspNetCore code/features used in this example

- Adding the AuthPermissions into your ASP.NET Core application.
- Bulk loading AuthPermissions Roles and Users. 
- Mapping the user's Roles to Permissions.
- Authorization in razor pages via the `[HasPermission(<enum permission>)]` attribute on the `PageModel` class.
- Authorization in razor pages via the `User.HasPermission(<enum permission>)` method.
- Add SuperUser on startup feature.

This article (!!! LINK !!!) details how this example was built, and how it works.

NOTE: This example does not include the admin pages for 

*NOTE: [This article](https://blog.francium.tech/asp-net-core-basic-authentication-authorization-in-razor-pages-with-postgresql-b1f2888b21d0) provides a good overview of the standard ASP.NET Core authorization approaches.*

