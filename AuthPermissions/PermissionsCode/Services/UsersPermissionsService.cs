// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace AuthPermissions.PermissionsCode.Services
{
    /// <summary>
    /// This will provide the names of the permission in the current user
    /// </summary>
    public class UsersPermissionsService : IUsersPermissionsService
    {
        private readonly IAuthPermissionsOptions _options;

        public UsersPermissionsService(IAuthPermissionsOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// This returns all the permissions in the provided ClaimsPrincipal (or empty list if no user or permission name)
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public List<string> PermissionsFromClaims(ClaimsPrincipal user)
        {
            var packedPermissions =
                user?.Claims.SingleOrDefault(c => c.Type == PermissionConstants.PackedPermissionClaimType)?.Value;

            var permissionNames = new List<string>();
            if (packedPermissions == null)
                return permissionNames;

            foreach (var permissionChar in packedPermissions)
            {
                var enumName = Enum.GetName(_options.EnumPermissionsType, (ushort) permissionChar);
                if (enumName != null)
                    permissionNames.Add( enumName);
            }

            return permissionNames;
        }

    }
}