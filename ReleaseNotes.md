# Release Notes

## Features

- Provide Role-to-Permissions authorization.
- Implements an JTW refresh token approach
- Provide multi-tenant features
- Works with any ASP.NET Core authentication provider.
- Works with either Cookie authentication or JWT Token authentication.
- Has admin services to sync the authentication provider users with  AuthP's users.
- Admin services to manage AuthP's Roles, Tenants and Users.

## 1.1.0-preview001

- BREAKING CHANGE: Different AuthRolesAdminService to be more useful
- BREAKING CHANGE: Different AuthUsersAdminService to be more useful
- Improvements to the Example AuthUsersController and RolesController
- BUG: Fixed bug in Example4 "sync users" feature
- BUG: Fixed bug in using in-memory database in an application

## 1.0.0-preview001

- Preview version - looking for feedback.

## Limitations of this release

- Only works for single instance of the web app (i.e. no scale out) _NOTE: The non-preview will fix this problem._

## Code still to do

- Improve "move tenant" to also move any data.
- Example 1 web site
  - Add Show/Edit/Sync users (simple)
  - Add show user's claims, show user's permissions


## Documentation etc. still needed

- Finish Wiki documentation using /docs folder
- Article 1 - Roles/Permissions - based on Example 1
- Article 2 - JWT token with refresh - based on Example 2
- Example 1,2,4 READMEs


