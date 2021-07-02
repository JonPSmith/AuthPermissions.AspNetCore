# Roadmap

## Phase 1 - First release of AuthPemissions

### Summary of new features

- Provide Role-to-Permissions authorization
- Proide multi-tenant features
- Implements an JTW refresh token approach
- Designed to work with all types of ASP.NET Core designs
  - Blazor, Razor pages, Web API, Web MVC (but see limitation on version 1)
- Works with any autheication provider that returns the user id as a string
- Works with any software architectures
  - Monoliths, microservices, serverless, containers etc.

### Limitations of this release

- Preview only: looking for feedback.
- Only meant for single instance of the web app (i.e. no scale out) *NOTE: Version 2 will fix this.*

## Phase 2 - Second release of AuthPemissions

### Summary of new features

- Has "only run once" library to manage migrations / seeding
- new Example3 - an example of using simple tenants with Azure Active Directory (MVC)
- Various small improvements 

### Limitations 

- Multi-tenant: A user can only have one tenant.

### Phase 2: things still to do 

- Create an ASP.NET Core "only run once" library using the [madelson/DistributedLock](https://github.com/madelson/DistributedLock) libraries.
- Add `HaveLoggedOut` event so that a logout on app using JWT will cause the refresh token to be removed.
- JWT Token: Encrypt added claims to JWT Token
  - How do we set up the encryption in general (look at IS)
  - Optional encryption of JWT claims
- IAddExtraClaims: Allow the user to create service that adds extra claims to Cookie/JWT

----

## Possible extra features

### Add PostgreSQL option for AuthP database

### Add SQLite option for AuthP database

### Build full admin features in Example 4

### Create a AuthPermissions admin scaffolding library

### Add refresh Cookie

Add per-HTTP Cookie checker - if the cookie is xxx old, then refresh claims.

Optionally use Cookie refresh database so than user can be rejected.

### AuthUser can have multiple tenants

- A user having multiple tenants: this needs
  - An three-step login code with tenant selection
- A user can have different roles on different tenants  
(the code is available in the UserToRoles, but the `DefineUserWithRolesTenant` class needs a `TenantNameForRoles` properly)

### Possible Security improvements

- Admin: An tenant admin user can only add Roles that the tenant admin user has
- Add logging to everything (especially anything that could go wrong)  

