// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.BaseCode.PermissionsCode;

namespace AuthPermissions.BaseCode.CommonCode
{
    /// <summary>
    /// This contains extension method about ASP.NET Core <see cref="Claim"/>
    /// </summary>
    public static class ClaimsExtensions
    {
        /// <summary>
        /// This returns the UserId from the current user's claims
        /// </summary>
        /// <param name="claims"></param>
        /// <returns>The UserId, or null if not logged in</returns>
        public static string GetUserIdFromClaims(this IEnumerable<Claim> claims)
        {
            return claims?.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// This returns the UserId from the current user 
        /// </summary>
        /// <param name="user">The current ClaimsPrincipal user</param>
        /// <returns>The UserId, or null if not logged in</returns>
        public static string GetUserIdFromUser(this ClaimsPrincipal user)
        {
            return user?.Claims.GetUserIdFromClaims();
        }


        /// <summary>
        /// This returns the AuthP packed permissions. Can be null if no user, or not packed permissions claims
        /// </summary>
        /// <param name="user">The current ClaimsPrincipal user</param>
        /// <returns>The packed permissions, or null if not logged in</returns>
        public static string GetPackedPermissionsFromUser(this ClaimsPrincipal user)
        {
            return user?.Claims.SingleOrDefault(x => x.Type == PermissionConstants.PackedPermissionClaimType)?.Value;
        }

        /// <summary>
        /// This returns the AuthP DataKey. Can be null if AuthP user has no user, user not a tenants, or tenants are not configured
        /// </summary>
        /// <param name="user">The current ClaimsPrincipal user</param>
        /// <returns>The AuthP DataKey from the claim, or null if no DataKey claim</returns>
        public static string GetAuthDataKeyFromUser(this ClaimsPrincipal user)
        {
            return user?.Claims.SingleOrDefault(x => x.Type == PermissionConstants.DataKeyClaimType)?.Value;
        }

        /// <summary>
        /// Returns the ConnectionName claim. Can be null if no user, user not a tenants or sharding isn't configured
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static string GetDatabaseInfoNameFromUser(this ClaimsPrincipal user)
        {
            return user?.Claims.SingleOrDefault(x => x.Type == PermissionConstants.DatabaseInfoNameType)?.Value;
        }
    }
}