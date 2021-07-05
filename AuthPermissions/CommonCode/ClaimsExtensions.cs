// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AuthPermissions.PermissionsCode;

namespace AuthPermissions.CommonCode
{
    public static class ClaimsExtensions
    {
        /// <summary>
        /// This returns the UserId from the current user (
        /// </summary>
        /// <param name="claims"></param>
        /// <returns></returns>
        public static string GetUserIdFromClaims(this IEnumerable<Claim> claims)
        {
            return claims?.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// This returns the AuthP packed permissions. Can be null if no user, or not packed permissions claims
        /// </summary>
        /// <param name="user">The current ClaimsPrincipal user</param>
        /// <returns>The packed permissions</returns>
        public static string GetPackedPermissionsFromUser(this ClaimsPrincipal user)
        {
            return user?.Claims.SingleOrDefault(x => x.Type == PermissionConstants.PackedPermissionClaimType)?.Value;
        }

        /// <summary>
        /// This returns the AuthP DataKey. Can be null if AuthP user has no tenant, or tenants are not configured
        /// </summary>
        /// <param name="user">The current ClaimsPrincipal user</param>
        /// <returns>The AuthP DataKey from the claim, or null if no DataKey claim</returns>
        public static string GetAuthDataKeyFromUser(this ClaimsPrincipal user)
        {
            return user?.Claims.SingleOrDefault(x => x.Type == PermissionConstants.DataKeyClaimType)?.Value;
        }
    }
}