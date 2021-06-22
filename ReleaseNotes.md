# Release Notes

## Known bugs

- Bulk load of hierarchical tenants provides a poor error message if a layer out (e.g using Company | West Coast | LA when you haven't defined "West Coast")

## Examples to add

- Build admin for 
  - role/permission
  - Tenants (with move)
  - Users
- Add roles, user admin to Example 1
- Example3 - an example of using simple tenants with Azure Active Directory (MVC)
- Example4 - hierarchical tenants with individual users (MVC)

## Add/improve new features

- Create an ASP.NET Core "only run once" library using the [madelson/DistributedLock](https://github.com/madelson/DistributedLock) libraries.
- Provide more configuration options/features
  - Don't migrate the AuthPermission database on startup
  - Delete Expired Refresh Tokens on startup
  - Optional Encript claims in JWT Token
- Improve IFindUserId to IFindUser (ID and name)
- Update AuthUsers with missing users (service and on startup)

- Add checks at the end of registering the data, e.g. if using tenants all user must have a tenant

## Possible Security improvements
- Option to encrypt the Auth claims in JWT token
- Add DisableUser to AuthUser - stops adding claims to ClaimsPricipal
- Add per-HTTP Cookie checker - if the cookie is xxx old, then refresh claims
- Add SecurityData to AuthUser and provide a method to 
  - be called when user logs in for the first time
  - be called whenever the JWT Token refresh (and Cookie xxx old). Allows you to log them out if security issue is found.



## Things not in the first release

- A user having multiple tenants: this needs
  - An three-step login code with tenant selection
- A user can have different roles on different tenants  
(the code is available in the UserToRoles, but the `DefineUserWithRolesTenant` class needs a `TenantNameForRoles` properly)