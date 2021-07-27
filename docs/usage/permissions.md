# Using Permissions to control access

This permission claim is available for the current user via the `ClaimsPrincipal`, which is in the HTTP context under the property `User` - See [this section for a diagram](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/concepts/AuthUser.md#how-are-the-authp-claims-used-once-a-user-is-logged-in). This claim can be used in two ways to control access to features in your application.

### 1. Using `HasPermission` attribute

The [HasPermission] attribute works with best with:

- ASP.NET CoreWeb API Controllers - [see this example](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/WeatherForecastController.cs).
- ASP.NET Core MVC Controllers - [see this example](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.MvcWebApp.IndividualAccounts/Controllers/ShopController.cs).
- ASP.NET Core Razor Pages - [see this example](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example1.RazorPages.IndividualAccounts/Pages/AuthPermissions/NeedsPermission1.cshtml.cs).

Here is a example taken from [Example2’s WeatherForecastController](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/WeatherForecastController.cs), which is Web API controller – see the first line.

```c#
[HasPermission(PermissionEnum.ReadWeather)]
[HttpGet]
public IEnumerable<WeatherForecast> Get()
{
    //… other code left out
}
```

### 2. Using `HasPermission` extension method

The other approach is to use the `HasPermission` extension method, which returns a true if the current user has the specific permission you are looking for more versatile. To works best with:

- Within Razor pages to control whether a feature should be displayed - [see this example](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.MvcWebApp.IndividualAccounts/Views/Shared/_Layout.cshtml#L38L58).
- 



If you are using Blazor, or in any Razor file you can use the HasPermission extension method to check if the current ASP.NET Core’s User has a specific Permission. Here is an example taken from AuthP’s [Example1 Razor Pages application](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example1.RazorPages.IndividualAccounts/Pages/AuthPermissions/NeedsPermission2.cshtml.cs).

```c#
public class SalesReadModel : PageModel
{
    public IActionResult OnGet()
    {
        if (!User.HasPermission(Example1Permissions.SalesRead))
            return Challenge();

        return Page();
    }
}
```

The `HasPermission` extension method is also useful in any Razor page (e.g. `User.HasPermission(Example.SalesRead)`) to decide whether a link/button should be displayed. In Blazor the call would be `@context.User.HasPermission(Example.SalesRead)`.



