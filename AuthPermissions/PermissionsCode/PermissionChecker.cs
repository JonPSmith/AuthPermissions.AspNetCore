// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;

[assembly: InternalsVisibleTo("Test")]
namespace AuthPermissions.PermissionsCode
{
    /// <summary>
    /// 
    /// </summary>
    public static class PermissionChecker
    {
        /// <summary>
        /// This returns true if the current user has the permission
        /// </summary>
        /// <param name="user"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public static bool UserHasThisPermission<TEnumPermissions>(this ClaimsPrincipal user, TEnumPermissions permission)
            where TEnumPermissions : Enum
        {
            var permissionClaim =
                user?.Claims.SingleOrDefault(x => x.Type == PermissionConstants.PackedPermissionClaimType);
            return permissionClaim?.Value.UserHasThisPermission(permission) == true;
        }

        /// <summary>
        /// This is used by the policy provider to check the permission name string
        /// </summary>
        /// <param name="enumPermissionType"></param>
        /// <param name="packedPermissions"></param>
        /// <param name="permissionName"></param>
        /// <returns></returns>
        public static bool ThisPermissionIsAllowed(this Type enumPermissionType, string packedPermissions, string permissionName)
        {
            var permissionAsChar = (char)Convert.ChangeType(Enum.Parse(enumPermissionType, permissionName), typeof(char));
            return IsThisPermissionAllowed(packedPermissions, permissionAsChar);
        }


        //-------------------------------------------------------
        //private methods
        private static bool UserHasThisPermission<TEnumPermissions>(this string packedPermissions, TEnumPermissions permissionToCheck)
            where TEnumPermissions : Enum
        {
            var permissionAsChar = (char)Convert.ChangeType(permissionToCheck, typeof(char));
            return IsThisPermissionAllowed(packedPermissions, permissionAsChar);
        }

        private static bool IsThisPermissionAllowed(string packedPermissions, char permissionAsChar)
        {
            return packedPermissions.Contains(permissionAsChar) ||
                   packedPermissions.Contains(PermissionConstants.PackedAccessAllPermission);
        }
    }
}