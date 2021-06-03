// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions.DataKeyCode;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupCode;
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
        private readonly ICalcAllowedPermissions _calcAllowedPermissions;
        private readonly ICalcDataKey _calcDataKey;


        /// <summary>
        /// Needs UserManager and IdentityOptions, plus the two services to provide the permissions and dataKey
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="optionsAccessor"></param>
        /// <param name="calcAllowedPermissions"></param>
        /// <param name="calcDataKey"></param>
        public AddPermissionsToUserClaims(UserManager<IdentityUser> userManager, IOptions<IdentityOptions> optionsAccessor, 
            ICalcAllowedPermissions calcAllowedPermissions, ICalcDataKey calcDataKey)
            : base(userManager, optionsAccessor)
        {
            _calcAllowedPermissions = calcAllowedPermissions;
            _calcDataKey = calcDataKey;
        }

        /// <summary>
        /// This adds the permissions and, optionally, a multi-tenant DataKey to the claims
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(IdentityUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            var userId = identity.Claims.GetUserIdFromClaims();
            var permissions = await _calcAllowedPermissions.CalcPermissionsForUserAsync(userId);
            identity.AddClaim(new Claim(PermissionConstants.PackedPermissionClaimType, permissions));
            var dataKey = await _calcDataKey.GetDataKeyAsync(userId);
            if (dataKey != null)
            {
                identity.AddClaim(new Claim(PermissionConstants.DayaKeyClaimType, dataKey));
            }
            return identity;
        }
    }

}