// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AuthPermissions.AspNetCore.Services
{
    /// <summary>
    /// This version provides:
    /// - Adds Permissions and DataKey claims to the user's claims.
    /// </summary>
    // Thanks to https://korzh.com/blogs/net-tricks/aspnet-identity-store-user-data-in-claims
    public class AddPermissionsToUserClaims : UserClaimsPrincipalFactory<IdentityUser>
    {
        private readonly IClaimsCalculator _claimsCalculator;

        /// <summary>
        /// Needs UserManager and IdentityOptions, plus the two services to provide the permissions and dataKey
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="optionsAccessor"></param>
        /// <param name="claimsCalculator"></param>
        public AddPermissionsToUserClaims(UserManager<IdentityUser> userManager, IOptions<IdentityOptions> optionsAccessor,
            IClaimsCalculator claimsCalculator)
            : base(userManager, optionsAccessor)
        {
            _claimsCalculator = claimsCalculator;
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
            var claims = await _claimsCalculator.GetClaimsForAuthUserAsync(userId);
            identity.AddClaims(claims);

            return identity;
        }
    }

}