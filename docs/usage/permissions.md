# Using Permissions to control access

This permission claim is available for the current user via the `ClaimsPrincipal`, which is in the HTTP context under the property `User` - See [this section for a diagram](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/concepts/AuthUser.md#how-are-the-authp-claims-used-once-a-user-is-logged-in). This claim can be used in three ways to control access to features in your application.

## 1. Using `HasPermission` attribute

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

## 2. Using `HasPermission` extension method

The other approach is to use the `HasPermission` extension method, which returns a true if the current user has the specific permission you are looking for. This is more versatile, but you have to write more code. This works best on:

- Within Razor pages to control whether a feature should be displayed - [see this example](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.MvcWebApp.IndividualAccounts/Views/Shared/_Layout.cshtml#L38L58).
- Inside Razor Page methods or Controller actions - [see this Razor Page example](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example1.RazorPages.IndividualAccounts/Pages/AuthPermissions/NeedsPermission2.cshtml.cs)
- In Blazor front-end code, e.g., `@context.User.HasPermission(Example.SalesRead)` will return true if the current user has that permission.

Here is an example taken from AuthP’s [Example1 Razor Pages application](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example1.RazorPages.IndividualAccounts/Pages/AuthPermissions/NeedsPermission2.cshtml.cs).

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

## 3. Using the `IUsersPermissionsService` service

If you are using a front-end library such as React, Angular, Vue and so on, then your front-end needs to know what Permissions the current user has so that the front-end can display the links, buttons etc. that the current user has access to. If you need this you need to set up a WebAPI that will return the current user's permissions.

The `IUsersPermissionsService` service has a method called `PermissionsFromUser` which returns a list of the Permission names for the current user (or null if no one is logged in or the user is not registered as an `AuthUser`). The code below comes from [Example2's AuthenticateController](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/AuthenticateController.cs#L112L122).

```c#
/// <summary>
/// This returns the permission names for the current user (or null if not available)
/// </summary>
/// <param name="service"></param>
/// <returns></returns>
[HttpGet]
[Route("getuserpermissions")]
public ActionResult<List<string>> GetUsersPermissions([FromServices] IUsersPermissionsService service)
{
    return service.PermissionsFromUser(User);
}
```

_NOTE: You only need to read this one login if using Cookie Authentication, and after login and refresh of a JWT Token. Thats because the user's permissions are recalculated at these points._

## Additional resources

- [Setting up your Permissions](!!!!)
- [Admin -> Roles/Permissions](!!!!)
