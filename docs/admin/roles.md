# AuthP's Roles admin services

An AuthP's Role provides a user (human or machine) with a list of AutHP Permissions which allow you to access certain pages / WebAPIs etc. in your application . Roles are normally named after the user's job, say "Sales Person", "Sales Manager", and so on.

The `IAuthRolesAdminService` provides methods to allow you to create, read, update, and delete (CRUD) AuthP's Roles, plus the ability to:

- List/filter all the Permissions with its extra data (see [Permission's `Display` data](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/docs/setup/permissions.md#adding-the-display-attribute)).
- Find all the AuthP's user that have a specific Role

**Please look at the [`IAuthRolesAdminService`](https://github.com/JonPSmith/AuthPermissions.AspNetCore/blob/main/AuthPermissions/AdminCode/IAuthRolesAdminService.cs) interface for the details on how these methods work.**.

_NOTE: Many of these methods in this service return a status that tells you if the method was successful (i.e. `status.IsValid` is `true`), or if there are errors then `status.Errors` contain the errors._

## What is in a AuthP's Role

An AuthP's Role is stored in a `RoleToPermissions` entity class and has of three parameters:

| Parameter | What | Type etc. |
| --------- | ---- | --------- |
| RoleName | This holds the name of the Role | string, non-null, unique |
| PackedPermissions | Compacts the Role's Permissions into a string | string, non-null |
| Description | Optional description of the Role | string, nullable |

_NOTE: The `QueryRoleToPermissions` method in the `IAuthRolesAdminService` returns a `IQueryable<DTO>` which adds an extra property `PermissionNames` which contains a list of the permissions names. This makes it easer for the admin user to understand what Permissions are in a Role._
