# Release Notes

## 2.0.0

- BREAKING CHANGE: The SetupAspNetCoreAndDatabase configuration method uses a different approach that supports multiple instances of your app
- BREAKING CHANGE: Changes the bulk loading of role and tenants to support the new multi-tenant Roles feature
- BREAKING CHANGE: Updated to net6.0 only
- MULTI-TENANT BREAKING CHANGE: The DataKey format has changed, You need to migrate your application - see issue 
- MULTI-TENANT BREAKING CHANGE: The RoleAdmin method QueryRoleToPermissions now needs the logged-in userId in multi-tenant applications
- New features: Each multi-tenant can have a different version, e.g. Free, Pro, Enterprise -  see issue #9
- New features: A Tenant Admin user can't see "Advanced Roles", i.e. Role that only an App Admin user should use - see issue #9
- New features: Uses Net.RunMethodsSequentially library to handle startup migrate / seed of databases for applications have mutiple instances running

## 1.4.0

New Feature: Added IndividualAccountsAuthentication<TCustomIdentityUser> and AddSuperUserToIndividualAccounts<TCustomIdentityUser> to handle Individual Accounts provider with a custom IdentityUser. See https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki/Setup-Authentication for more info.


## 1.3.0

- BREAKING CHANGE: When registering AuthP you need to state what authentication provider you are using - see https://github.com/JonPSmith/AuthPermissions.AspNetCore/wiki/Authentication-explained
- New Feature: New AzureAdAuthentication method that causes an Azure AD via Login will add AuthP claims to the user. See Example5 for for how this works.


## 1.2.1

- Bug fix: UpdateUserAsync didn't handle no roles properly
- Change: UpdateUserAsync method parameter roleNames cannot be null

## 1.2.0

- BREAKING CHANGE: Different AuthTenantAdminService to be more useful
- New Feature: Added ITenantChangeService to apply tenant changes to application DbContext
- Updated Microsoft's NuGets to fix a security issue in example2

## 1.1.0

- BREAKING CHANGE: Different AuthRolesAdminService to be more useful
- BREAKING CHANGE: Different AuthUsersAdminService to be more useful
- Improvements to the Example AuthUsersController and RolesController
- BUG: Fixed bug in Example4 "sync users" feature
- BUG: Fixed bug in using in-memory database in an application

## 1.0.0-preview001

- Preview version - looking for feedback.




