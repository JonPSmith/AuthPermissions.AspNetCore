# Explaining AuthP's JWT Token security feature

ASP.NET Core supports JWT Tokens for authentication (see [this Microsoft docs](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/)). JWT Tokens work well with ASP.NET Core WebAPI systems and Microservices. But JWT Tokens have a couple of security concerns:

1. JWT Tokens, by default, live for a long time otherwise the user would need to log in again. This means if a hacker gets a copy of your JWT Token then they can access the application as if they are you.
2. Any data in the JWT Token are, by default, not encrypted.

## 1. A solution to the long lifetime of the JWT Token

The recommended way to deal with the long lifetime is to use what is called a _JWT Refresh Token_. Many people have written about this, and the AuthP JWT Refresh Token version is based on [Rui Figueiredo](https://www.blinkingcaret.com/2018/05/30/refresh-tokens-in-asp-net-core-web-api/) and [Mohamad Lawand](https://dev.to/moe23/refresh-jwt-with-refresh-tokens-in-asp-net-core-5-rest-api-step-by-step-3en5) articles.

The JWT Refresh Token approach makes the lifetime of the JWT Token short (say minutes instead of the normal hours), and provides a unique refresh value. So, when the JWT Token lifetime has expired the front-end code sends the expired JWT Token with the unique refresh value to a refresh point. The backend then returns a new JWT Token and new unique refresh value and the user can continue to access the application. From the user's point of view they are logged in all the time, while in fact re-authorizations are happening in the background.

The diagram below shows how this works:

![Extract Permission Claims](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/images/JWTRefreshProcess.png)

_NOTE: Example2 is a ASP.NET Core WebAPI app which implements this process - have a look at the [AuthenticateController](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/AuthenticateController.cs) which shows the normal JWT Token approach and the JWT Refresh Token approach._

### How does improve the security of a using a JWT Token?

The JWT Refresh Token approach improves the security in the following ways:

- If a hacker manages to copy your JWT Token it is only valid for an short time (you set how long it is valid).
- The JWT Refresh value isn't send every time (its only sent on login and refresh). This means a hacker is less likely to capture the JWT Refresh value.
- The JWT Refresh value can only be used once, unlike the JWT Token. This means the hacker would have to capture the latest JWT Refresh value and use it before the valid user does.
- The AuthP library has a `IDisableJwtRefreshToken` service which allows you to invalidate the JWT Refresh value for a user. You can call this a) when a user logs out, or b) you want to log out an active user when there is some suspicious activity.

_NOTE: One extra security feature that the AuthP implementation provides is that the user's claims are updated on every refresh. For instance if an admin person changed any Roles or Permissions that effect the user being refreshed, then their Permissions and DataKey claims are updated._

## 2. A solution for the data in the JWT Token isn't encrypted

At this point in time I have NOT added the option to encrypt any claims in JWT Token, but I could add this in a later version. I'm looking for feedback on whether this would be useful. Let me know via an issue.

**Be warned:** Encryption is easy on standard ASP.NET Core deployments, but Microservices or the use of Container takes  a bit more work.

## Additional resources

- [Setting up your JWT Token](!!!!)