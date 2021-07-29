# Setting up your Permissions

The [Introduction to AuthP's Permissions](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/concepts/permissions.md) covers why we use a C# Enum members to secure access to pages, WebAPIs, links etc. This page describes how to build a Enum in a way that is going to work with the AuthP library, and tips on how to organize the Enum members so that they don't become a ["big ball of mud"](http://www.laputan.org/mud/) when future additions are added.

## Recommendations on how to organize your permissions

Some years ago I designed a large application using this Roles/Permissions approach for a client which taught me a lot about how the permissions should be organize. The list below list what I learnt.

- **Features come in groups**: A feature we want to secure often has more than one part of it, e.g. a selling feature might have
  - SalesRead - user can see what has been sold.
  - SalesSell - user can use the app to sell something.
  - SalesReturn - user can process a return of a previous sale
- **Feature groups should be easy to find**: When you have hundreds of feature parts managing it gets harder.
- **Feature groups grow over time**: This means we need a way to add new feature easily
- **Don't delete Obsolete feature parts**: If a feature part is obsolete, then don't delete it as its number could be reused, which could cause problems.
- **Adding extra info helps your admin staff**: Just a name, like `SalesRead` can be ambiguous, so more data is needed.
- **You need a SuperAdmin override**: When you are setting up a new application you need an SuperAdmin override to access anything so that they can set up normal admin users.

The next section describes the various approaches used in the AuthP library that come from the above observations. 

## The various parts of the permissions Enum

The following sections describe each part of the Permission Enum.

_NOTE: If you prefer to see the actual Permissions with comments see the [Example4Permissions](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/Example4.MvcWebApp.IndividualAccounts/PermissionsCode/Example4Permissions.cs) example, which has every part of a AuthP's Permissions Enum.

### Creating the Permissions Enum

The code below shows how you would define your Permissions Enum off. The three rules shown in the code are required.

```c#
public enum YourPermissions : ushort //1. Must be ushort to work with AuthP
{
    NotSet = 0,                      //2. You mustn't use a zero as a valid member for 
    // ... other code left out

    AccessAll = ushort.MaxValue      //3. This allows a user to access every feature
}
```

_NOTE: The `AccessAll` Permission member is very powerful and should only be given to a SuperAdmin user. The SuperUser can create normal admin users which have most of the Permission members, but not the `AccessAll` Permission._ 

### Grouping into a feature group

- The first thing is to group individual parts of a feature into a **Group*. See the Sales group
- I define a number for each enum. That's important as if you add a new Enum member in a earlier group.
- I use sequential numbers because I don't want to duplicate a number somewhere else.
- I leave a gap in the numbers (e.g. start the next group at 20, or 100) to allow for new members to be added to a group

```c#
public enum YourPermissions : ushort 
{
    // ... other code left out 
    
    //The Sales group
    SalesRead = 10,
    SalesSell = 11,
    SalesReturn = 12,

    //The next group
    AnotherPermission = 20,
    
    // ... other code left out

}
```

_NOTE: The Permissions above WON'T display in the admin display. The code above is only to show the grouping and numbering. See next section for the recommended member format._

### Adding the `Display` attribute

To provide more information for the admin (a developers) about each Permission member.  
_NOTE: You must define the `GroupName`, but the `Name` and `Description` are optional._

```c#
public enum YourPermissions : ushort 
{
    // ... other code left out 
    
    //The Sales group
    [Display(GroupName = "Sales", Name = "Read", Description = "Can read any sales")]
    SalesRead = 10,
    [Display(GroupName = "Sales", Name = "Sell", Description = "Can sell items from stock")]
    SalesSell = 11,
    [Display(GroupName = "Sales", Name = "Return", Description = "Can return an item to stock")]
    SalesReturn = 12,

    // ... other code left out
}
```

Here is an example of how Permissions are shown to an admin person using `AuthRolesAdminService.GetPermissionDisplay` method.  
_NOTE: The `GetPermissionDisplay` can filter by GroupName`._

![Permission Admin display](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/images/ListPermissions.png)

### How to obsolete a Permission member

As stated at the start you need a way to obsolete a Permission member. Of course, is to add the `[Obsolete]` attribute - see code below.

```c#
//----------------------------------------------------
//This is an example of what to do with permission you don't used anymore.
//You don't want its number to be reused as it could cause problems 
//Just mark it as obsolete and the PermissionDisplay code won't show it
[Obsolete]
[Display(GroupName = "Old", Name = "Not used", Description = "example of old permission")]
OldPermissionNotUsed = 1_000,
```

This means it won't display in the admin Permissions display, and all uses of obsoleted Permission member will have a intellisense message saying its obsolete.

### Adding Permission members before a feature is ready

Sometimes you want to add new Permission members, but they shouldn't be used by anyone yet. If you don't add a `[Display]` attribute it won't be shown the PermissionDisplay.

```c#
// A enum member with no <see cref="DisplayAttribute"/> can be used, but won't shown in the PermissionDisplay
// Useful if are working on new permissions but you don't want it to be used by anyone yet 
HiddenPermission = 2_000,
```

_NOTE: You can use hidden Permission members in unit tests, including in the bulk loading of Roles, but you can't add a hidden Permission member to an existing application using the normal AuthP admin tools._

### Filtering out advanced Permissions

For multi-tenant systems you might want an admin role to just manage the users in the specific tenant. AuthP's admin code can users by their DataKey, but you might want to remove some Permission members from a tenant-level admin person. By adding `AutoGenerateFilter = true` to the `Display` attribute that Permission member the permission display won't show that member.

The code below allows a tenant-level admin user to only see the `TenantList` Permission, but not the `TenantCreate` or `TenantUpdate` Permissions.

```c#
//42_000 - tenant admin
[Display(GroupName = "TenantAdmin", Name = "Read Tenants", Description = "Can list Tenants")]
TenantList = 42_000,
[Display(GroupName = "TenantAdmin", Name = "Create new Tenant", Description = "Can create new Tenants", AutoGenerateFilter = true)]
TenantCreate = 42_001,
[Display(GroupName = "TenantAdmin", Name = "Alter existing Tenants", Description = "Can update or move a Tenant", AutoGenerateFilter = true)]
TenantUpdate = 42_001,
```

## Additional resources

[Admin - Roles/Permissions](!!!!)