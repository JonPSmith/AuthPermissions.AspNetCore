# Release Notes

## Known bugs

- Bulk load of hierarchical tenants provides a poor error message if a layer out (e.g using Company | West Coast | LA when you haven't defined "West Coast")

### Features

- Provide Role-to-Permissions authorization
- Proide multi-tenant features
- Implements an JTW refresh token approach

### Limitations of this release

- Preview only: looking for feedback.
- Only meant for single instance of the web app (i.e. no scale out) *NOTE: Version 2 will fix this.*

### Code still needed

- Turn on/off applying migrations on startup
- Add concurrency checks to all AuthP entities + add to SaveChangesWithChecks
- IAddExtraClaims: Allow the user to create service that adds extra claims to Cookie/JWT 
- Finish the sync user example in Example4

## Documanation etc. still needed

- Article 1 - Roles/Permissions - based on Example 1
- Article 2 - JWT token with refresh - based on Example 2
- Good overall README
- Example 1,2,4 READMEs
- Example 1 web site
  - Add NavBar: Show all users, show user's claims, show user's permissions  
- Example 4 web site
  - Add NavBar: 
    - If Admin: Show all users, show user's claims, show user's permissions 
    - If Tenant: Show shop, show user's claims
  - Home Index - explain what the application does 
  - AuthUsers - get sync users working
- List of limitations and roadmap

