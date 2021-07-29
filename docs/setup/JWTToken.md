# Setting up a JWT Token (and optional JWT Refresh Token)

JWT Tokens are supported by ASP.NET Core and work well with WebAPI systems and Microservices. This page shows you how to set up a JWT Token that contains the AuthP's Permission, and optional multi-tenant DataKey claims, into a JWT Token. This page also contains information on how to set up AuthP's [JWT Refresh Token to improve security](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/concepts/JWTRefresh.md).

_NOTE: The [AuthPermissions Example2](https://github.com/JonPSmith/AuthPermissions.AspNetCore/tree/main/Example2.WebApiWithToken.IndividualAccounts) project is a ASP.NET WebAPI using JWT Token (and AuthP's JWT Refresh Token feature). You can try this application via its Swagger front-end. All the examples in this page are from that example.

## Configuring a JWT Token in ASP.NET Core

I haven't found any good Microsoft documentation on setting up a JWT Token for authentication. The best article on [setting up JWT Tokens in ASP.Net Core](https://weblog.west-wind.com/posts/2021/Mar/09/Role-based-JWT-Tokens-in-ASPNET-Core) I found was by Rick Strahl and I followed that (but changes think to match AuthP's approach to Roles/Permissions).

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

And the `appsetting.json` file contains the following json setting.

```json
{
  //... other settings removed

  //This contains the data needed for the jwt bearer token
  //It is in the appsetting so that these values can be overwritten when you deploy to production
  "JwtData": {
    "Issuer": "https://localhost:44304",
    "Audience": "https://localhost:44304",
    "SigningKey": "some-long-secret-key-that-is-NOT-in-your-appsetting-file" //Use user secrets, or override at deployment time
  }
}
```

## Creating the JWT Token

If you look at Rick Strahl article you will see he has to write a load code to create a valid JWT Token to return when the user logs in. The AuthP library provides a 