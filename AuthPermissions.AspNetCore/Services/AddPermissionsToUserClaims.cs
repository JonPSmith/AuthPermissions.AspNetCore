// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using FeatureAuthorize;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AuthPermissions.AspNetCore.Services
{
    /// <summary>
    /// This version provides:
    /// - Adds Permissions to the user's claims.
    /// </summary>
    // Thanks to https://korzh.com/blogs/net-tricks/aspnet-identity-store-user-data-in-claims
    public class AddPermissionsToUserClaims : UserClaimsPrincipalFactory<IdentityUser>
    {
        private readonly AuthPermissionsDbContext _authPermissionsDbContext;

        public AddPermissionsToUserClaims(UserManager<IdentityUser> userManager, IOptions<IdentityOptions> optionsAccessor,
            AuthPermissionsDbContext authPermissionsDbContext)
            : base(userManager, optionsAccessor)
        {
            _authPermissionsDbContext = authPermissionsDbContext;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(IdentityUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            var userId = identity.Claims.GetUserIdFromClaims();
            var rtoPCalcer = new CalcAllowedPermissions(_authPermissionsDbContext);
            identity.AddClaim(new Claim(PermissionConstants.PackedPermissionClaimType, await rtoPCalcer.CalcPermissionsForUserAsync(userId)));
            return identity;
        }
    }

}