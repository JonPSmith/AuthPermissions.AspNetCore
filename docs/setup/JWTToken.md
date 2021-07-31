# Setting up a JWT Token (and optional JWT Refresh Token)

JSON Web Token (JWT) Bearer Token (shortened to 'JWT Token') are supported by ASP.NET Core and work well with WebAPI systems and Microservices. This page shows you how to set up a JWT Token that contains the AuthP's Permission, and optional multi-tenant DataKey claims, into a JWT Token. This page also contains information on how to set up AuthP's [JWT Refresh Token to improve security](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/concepts/JWTRefresh.md).

_NOTE: The [AuthPermissions Example2](https://github.com/JonPSmith/AuthPermissions.AspNetCore/tree/main/Example2.WebApiWithToken.IndividualAccounts) project is a ASP.NET WebAPI using JWT Token (and AuthP's JWT Refresh Token feature). You can try this application via its Swagger front-end. All the examples in this page are from that example._

## Configuring a JWT Token in ASP.NET Core

I haven't found any good Microsoft documentation on setting up a JWT Token for authentication. The best article on [setting up JWT Tokens in ASP.Net Core](https://weblog.west-wind.com/posts/2021/Mar/09/Role-based-JWT-Tokens-in-ASPNET-Core) I found was by Rick Strahl and I followed that (but changes some things to match AuthP's approach to Roles/Permissions).

I recommend you read [Rick Strahl article](https://weblog.west-wind.com/posts/2021/Mar/09/Role-based-JWT-Tokens-in-ASPNET-Core), but here is the ASP.NET Core setup from [AuthPermissions Example2's Startup class](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Startup.cs), but some JWT Refresh Token setup removed.

```c#
var jwtData = new JwtSetupData();
Configuration.Bind("JwtData", jwtData);
services.AddAuthentication(auth =>
    {
        auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        auth.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtData.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtData.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtData.SigningKey)),
        }
    });
```

And the `appsetting.json` file contains the following json setting. This contains the data needed for the JWT Token. It is in the appsetting so that these values can be overwritten when you deploy to production.

```json
{
  "JwtData": {
    "Issuer": "https://localhost:44304",
    "Audience": "https://localhost:44304",
    "SigningKey": "some-long-secret-key-that-is-NOT-in-your-appsetting-file" 
  }
}
```

_NOTE: The "SigingKey" is a important value that must be kept secret. When you are deploying to production you should either this value during deployment, or use user secrets._

## Creating the JWT Token (without JWT refresh feature)

If you look at Rick Strahl article you will see he has to write a load code to create a valid JWT Token to return when the user logs in. The AuthP library provides a `ITokenBuilder` service which builds a JWT Token for you. This service can then be used in your authentication code. You can see an example of an WebAPI authentication method using the `ITokenBuilder` service in the [`Authenticate` method in Example2's `AuthenticateController`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/AuthenticateController.cs#L29L48). The the `ITokenBuilder` service creates JWT Token with the UserId Claim, the AuthP's Permissions claim, and if you are using multi-tenant feature, the DataKey claim. 

If you want to use the `ITokenBuilder` service you must set up the `AuthPermissionsOptions.ConfigureAuthPJwtToken` data with a new `AuthPJwtConfiguration` class with the same data as your used in setting up the JWT Token with ASP.NET Core. You also need to provide a the length of time before the JWT Token expires (NOTE: See JWT Refresh approach later in this page).

```c#
services.RegisterAuthPermissions<Example2Permissions>( options =>
    {
        options.MigrateAuthPermissionsDbOnStartup = true;
        options.ConfigureAuthPJwtToken = new AuthPJwtConfiguration
        {
            Issuer = jwtData.Issuer,
            Audience = jwtData.Audience,
            SigningKey = jwtData.SigningKey,
            TokenExpires = new TimeSpan(2, 0, 0, 0), //The JWT Token will last for 2 days
        };
    })
    //... other AuthP configurations left out
```

_NOTE: If you want to create your own JWT Token then you can. In this case you don't have set the `AuthPermissionsOptions.ConfigureAuthPJwtToken`, but for AuthP to work you need to include the AuthP claims. You can get these via AuthP's `IClaimsCalculator` service._

## Creating the JWT Token WITH JWT refresh feature

In the [Explanation of the JWT refresh feature](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/concepts/JWTRefresh.md) I cover why using a JWT refresh approach improved the security of using JWT Tokens. To use AuthP's JWT refresh feature you have to:

1. Alter `ConfigureAuthPJwtToken` configuration data
2. Change the WebAPI authentication method.
3. Add a WebAPI JWT Refresh method

### 1. Alter `ConfigureAuthPJwtToken` configuration data

When using the JWT refresh feature you want the:

- JWT Token to expires quickly - say minutes rather than days
- You need to define how long the JWT refresh value is still valid

In AuthP configuration shown below shows this

```c#
services.RegisterAuthPermissions<Example2Permissions>( options =>
    {
        options.MigrateAuthPermissionsDbOnStartup = true;
        options.ConfigureAuthPJwtToken = new AuthPJwtConfiguration
        {
            Issuer = jwtData.Issuer,
            Audience = jwtData.Audience,
            SigningKey = jwtData.SigningKey,
            TokenExpires = new TimeSpan(0, 5, 0), //The JWT Token will last for 5 minutes
            RefreshTokenExpires = new TimeSpan(1,0,0,0) //Refresh token is valid for one day
        };
    })
    //... other AuthP configurations left out
```

_NOTE: Look at the ASP.NET Core JWT Token setup part [Example2 `Startup` class](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Startup.cs#L47L80) and you will see there is useful event that [Rui Figueiredo](https://www.blinkingcaret.com/2018/05/30/refresh-tokens-in-asp-net-core-web-api/) suggests. You might find that useful._

### 2. Change your WebAPI authentication method

The WebAPI authentication method now needs to return both the JWT Token and the JWT Refresh value. You can see this in the [`AuthenticateWithRefresh` method in Example2's `AuthenticateController`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/AuthenticateController.cs#L62L81).

### 3. Add a WebAPI JWT Refresh method

When front-end code detects that the JWT Token it needs to go to a different authentication method to refresh (see the diagram in [this page](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/concepts/JWTRefresh.md)). This takes in the old JTW Token and the JWT Refresh value and, if they are valid, it will sent back a new JTW Token and the new JWT Refresh value.

You can see the [`RefreshAuthentication` method in Example2's `AuthenticateController`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/AuthenticateController.cs#L95L110).

## Sending the current user's Permissions to the front-end

I might want your front-end code to have access to the current user's Permissions. This would allow the front-end to only show links that the current user can access. You can get the current user's Permissions via the `IUsersPermissionsService`. See the [`GetUsersPermissions` method in Example2's `AuthenticateController`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/AuthenticateController.cs#L112L125).

The front-end should call this after:

- A login.
- When the JWT Token is refreshed.
