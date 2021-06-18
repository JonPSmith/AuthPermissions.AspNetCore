# Release Notes

## Examples to add

- Build admin role/permission code, e.g. add a user, add a role, alter a user's Roles
  - Make bulk load users/roles into services
- Example3 - an example of using tenants
- Build admin user/Tenant code, e.g. add a user with tenant, add a tenant, move a tenant
  - Make bulk load tenants into a service

## Add/improve new features

- Create an ASP.NET Core "only run once" library using the [madelson/DistributedLock](https://github.com/madelson/DistributedLock) libraries.
- Provide more configuration options/features
  - Don't migrate the AuthPermission database on startup
  - Delete Expired Refresh Tokens on startup
  - xx

## Things not in the first release

- A user having multiple tenants: this needs
  - An three-step login code with tenant selection
- A user can have different roles on different tenants  
(the code is available in the UserToRoles, but the `DefineUserWithRolesTenant` class needs a `TenantNameForRoles` properly)