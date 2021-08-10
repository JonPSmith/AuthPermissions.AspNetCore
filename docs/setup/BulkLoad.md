# Bulk Load of AuthP settings

The AuthP database contains AuthP's `Roles`, (multi-)`Tenants` and `AuthUsers`. These can all be set up using various [admin services](!!!!), but that requires an admin person to add each `Role`, (multi-)`Tenant` or `AuthUser` one at a time. This can be time-consuming in situations such as:

- Adding AuthP features to an existing application with lots of existing users.
- Adding new groups of users, say a new company wants to use your multi-tenant system.
- When you are running unit tests, integration tests, or acceptance tests.

This is why the AuthP library contains various methods/services to load multiple `Roles`, (multi-)`Tenants` and `AuthUsers`. There are two versions:

- ????????????????????????????????????????????