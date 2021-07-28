# Explaining AuthP's Permissions

To manage access to features in an ASP.NET Core application uses a concept that AuthP called _Permissions_. Each Permission defines a flag that can be used to allow access to a feature.

Permissions could be strings (just like ASP.NET Roles are), but I found a C# Enum was best for the following reasons:

- Using an Enum means IntelliSense can prompt you as to what Enum names you can use. This stops the possibility of typing an incorrect Permission name.
- Its easier to find where a specific Permission is used using Visual Studio’s “Find all references”.
- You can provide extra information to an Enum entry using attributes. The extra information helps the admin person when looking for a Permission to add to a AuthP’s Role.
- A C# Enum also has a integer value, which allows a long list of Permissions can be stored more efficiently. This is important as the user's Permissions have to become a claim.

## An example of defining a Permission

Below is a list of Permissions taken from the [Example4 Permissions](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.MvcWebApp.IndividualAccounts/PermissionsCode/Example4Permissions.cs). 

```c#
public enum Example4Permissions : ushort
{
    NotSet = 0, //error condition

    //Here is an example of very detailed control over the selling things
    [Display(GroupName = "Sales", Name = "Read", Description = "Can read any sales")]
    SalesRead = 20,
    [Display(GroupName = "Sales", Name = "Sell", Description = "Can sell items from stock")]
    SalesSell = 21,
    [Display(GroupName = "Sales", Name = "Return", Description = "Can return an item to stock")]
    SalesReturn = 22,

    [Display(GroupName = "Employees", Name = "Read", Description = "Can read company employees")]
    EmployeeRead = 30,

    //other Permissions left out… 
}
```

Here are some details about the format of an Enum when working with the AuthP library.

- Each permission is given a number rather than letting C# set a number. That's because you don't want to have the value of an Enum change if you add new Enum before existing Enums.
- The `Display` attribute to each Enum provides extra information to help the admin user when creating or changing a AuthP Role.

_NOTE: You will find a more detailed list of rules / characteristics for the Permissions Enums found in [Defining your Permissions](!!!!)._

## Example of using Permissions in your application

The [AuthUser overview](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/concepts/AuthUser.md#how-are-the-authp-claims-added-to-the-logging-in-user) explains how the `ClaimsPrincipal` user in ASP.NET Core gets a claim containing all the permissions that the current user has. There are three ways to use the user's Permission claim to control that user can access.

### 1. Using `HasPermission` attribute

For a ASP.NET Core MVC or Web API controller you can add the [HasPermission] attribute to an access method in a controller. Here is a example taken from [Example2’s WeatherForecastController](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/WeatherForecastController.cs), which is Web API controller – see the first line.

```c#
[HasPermission(PermissionEnum.ReadWeather)]
[HttpGet]
public IEnumerable<WeatherForecast> Get()
{
    //… other code left out
}
```

### 2. Using `HasPermission` extension method

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

### 3. Using the `IUsersPermissionsService` service

If you are using a front-end library such as React, Angular, Vue and so on, then your front-end needs to know what Permissions the current user has so that the front-end can display the links, buttons etc. that the current user has access to. If you need this you need to set up a WebAPI that will return the current user's permissions. 

The `IUsersPermissionsService` service has a method called `PermissionsFromUser` which returns a list of the Permission names for the current user (or null if no one is logged in or the user is not registered as an `AuthUser`). You can see the `IUsersPermissionsService` service in action in [Example2's AuthenticateController](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example2.WebApiWithToken.IndividualAccounts/Controllers/AuthenticateController.cs#L112L122).

## Additional resources

- [Defining your Permissions](!!!!)
- [Using Permissions in your application](!!!!)
- [Roles admin service](!!!!)