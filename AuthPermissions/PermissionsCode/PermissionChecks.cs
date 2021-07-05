// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using AuthPermissions.CommonCode;

[assembly: InternalsVisibleTo("Test")]
namespace AuthPermissions.PermissionsCode
{
    /// <summary>
    /// 
    /// </summary>
    public static class PermissionChecks
    {

        /// <summary>
        /// This returns true if the current user has the permission
        /// </summary>
        /// <param name="user"></param>
        /// <param name="permissionToCheck"></param>
        /// <returns></returns>
        public static bool HasPermission<TEnumPermissions>(this ClaimsPrincipal user, TEnumPermissions permissionToCheck)
            where TEnumPermissions : Enum
        {
            var packedPermissions = user.GetPackedPermissionsFromUser();
            if (packedPermissions == null)
                return false;
            var permissionAsChar = (char)Convert.ChangeType(permissionToCheck, typeof(char));
            return packedPermissions.IsThisPermissionAllowed(permissionAsChar);
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
            return packedPermissions.IsThisPermissionAllowed(permissionAsChar);
        }


        //-------------------------------------------------------
        //private methods

        private static bool IsThisPermissionAllowed(this string packedPermissions, char permissionAsChar)
        {
            return packedPermissions.Contains(permissionAsChar) ||
                   packedPermissions.Contains(PermissionConstants.PackedAccessAllPermission);
        }
    }
}