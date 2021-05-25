# Example1.RazorApp.IndividualAccounts

This project contains a example of using the AuthPermissions.AspNetCore library in ASP.NET Core razor page web app with user data provided by the individual accounts approach. This is one of the simplest approaches using:

- **Application**: ASP.NET Core, Razor Pages
- **App type**: Single instance with one database.
- **Database type**: SQL Server (localdb for testing)
- **Users**: ASP.NET Core's individual accounts
- **Roles**: ASP.NET Core's individual accounts
- **AuthenticationType**: Cookie

The ASP.NET Core code comes comes from the [ASP.NET Core documentation on building razor page web app individual accounts authorization](https://docs.microsoft.com/en-us/aspnet/core/security/authorization/secure-data), but the handling of the visibilty of the contact manager features are handled by the AuthPermissions.AspNetCore library.

The AuthPermissions.AspNetCore code/features used in this example

- Mapping the user's Roles to Permissions (read this doc).
- Authorization in razor pages via the `IsAuthorized(<enum permission>)` method.
- UserId data key, plus permissions.
- Add SuperUser on startup feature.
- Admin page to alter the permissions in each role.

*NOTE: [This article](https://blog.francium.tech/asp-net-core-basic-authentication-authorization-in-razor-pages-with-postgresql-b1f2888b21d0) provides a good overview of the statndard ASP.NET Core authorization approaches.*

