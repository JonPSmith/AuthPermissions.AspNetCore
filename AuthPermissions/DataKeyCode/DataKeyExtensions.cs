// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using AuthPermissions.PermissionsCode;

namespace AuthPermissions.DataKeyCode
{
    /// <summary>
    /// Useful extension methods relating to the Tenant DataKey
    /// </summary>
    public static class DataKeyExtensions
    {
        /// <summary>
        /// This returns the AuthP DataKey. Can be null if AuthP user has no tenant, or tenants are not configured
        /// </summary>
        /// <param name="user">The current ClaimsPrincipal user</param>
        /// <returns>The AuthP DataKey from the claim, or null if no DataKey claim</returns>
        public static string GetAuthDataKey(this ClaimsPrincipal user)
        {
            return user?.Claims.SingleOrDefault(x => x.Type == PermissionConstants.DataKeyClaimType)?.Value;
        }
    }
}