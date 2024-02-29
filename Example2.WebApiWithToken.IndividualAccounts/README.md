# Example2.WebApiWithToken.IndividualAccounts

This project contains a example of using the AuthPermissions.AspNetCore library in ASP.NET Core web api where the authentication/authorization is held in a JWT token. This example shows how the AuthPermissions.AspNetCore library can work with JWT tokens and provide a way improve security.

VIDEO: [JWT Token refresh](https://www.youtube.com/watch?v=DtfNUHgwKyU)

This the [JWT Token refresh explained](https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki/JWT-Token-refresh-explained) document details how WebAPI and the refresh token. 

- **Application**: ASP.NET Core web API
- **AuthorizationProvider**: ASP.NET Core's individual accounts
- **CookieOrToken**: JWT Token
- **MultiTenant**: not used
- **Databases**: One SQL Server database uses by two parts
  - Individual accounts
  - AuthPermissions.
- **Focused on**: The basics of the AuthPermissions authorization system.
- **Special features**: JWT Token with refresh feature.

Users that you can log in from (NOTE: the Password is the same as the the email)

| Email        | Permissions |
| ------------ | ------------- |
| NoP@g1.com   | - none -                               |  
| P1@g1.com    | Permission1							|
| P2@g1.com    | Permission1, Permission2				|
| P3@g1.com    | Permission1, Permission2, Permission3	|
| Super@g1.com | AccessAll								|

The ASP.NET Core project was created via Create new project > ASP.NET Core Web API with no Authentication. Then I added:

1. Startup.ConfigureServices method:
   1. Added Individual Accounts, e.g. add `AddDefaultIdentity<IdentityUser>...`
   2. Added JWT authentication, e.g. add `services.AddAuthentication(auth => ... JWTBearer`
   3. Added RegisterAuthPermissions, e.g. add `services.RegisterAuthPermissions<YourPermissionEnum>...` etc.
   4. Updated the Swagger code to allow the JWT Token can be added to the Swagger Authorize button.
2. Startup.Configure method:
   1. Added `app.UseAuthentication();`
3. appsettings.json file: Setup data for the JWT Token - see JwtData section.
4. Created a `AuthenticateController` which provided Web API methods that let a user log in and obtain a JWT Token (and optional a Refresh Token) to use in normal accesses to the other Web APIs.
5. Added a `[HasPermission(Example2Permissions.Permission1)]` attribute to the GET method in the `WeatherForecastController`, so that if the user didn't have `Permission1` the request will return an error.

### The Example2.WebApiWithToken.IndividualAccounts code/features used in this example

- A Web API to log in which returns a JWT Token
- A Web API to log in which returns a JWT Token and Refresh Token, with another Web API to refresh the JWT Token without the user from having to log in again.
- A simple .

### Useful articles about ASP.NET Core and JWT Token

- Rick Strahl's article [Role based JWT Tokens in ASP.NET Core APIs](https://weblog.west-wind.com/posts/2021/Mar/09/Role-based-JWT-Tokens-in-ASPNET-Core).
- Blinkingcaret article [Refresh Tokens in ASP.NET Core Web Api](https://www.blinkingcaret.com/2018/05/30/refresh-tokens-in-asp-net-core-web-api/) - old but still useful.
- More up-to-date article [Refresh JWT with Refresh Tokens in Asp Net Core 5 Rest API Step by Step](https://dev.to/moe23/refresh-jwt-with-refresh-tokens-in-asp-net-core-5-rest-api-step-by-step-3en5).

