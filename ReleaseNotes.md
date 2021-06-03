# Release Notes

## Things that are correct

- The UserTenantKey needs more work for hierarchical work

## Things not in the first release

- A user having multiple tenants: this needs
  - An three-step login code with tenat selection
- A user can have different roles on different tenants  
(the code is available in the UserToRoles, but the `DefineUserWithRolesTenant` class needs a `TenantNameForRoles` properly)