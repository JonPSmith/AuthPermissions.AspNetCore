// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AuthPermissions.CommonCode;

namespace AuthPermissions.PermissionsCode.Services
{
    /// <summary>
    /// This will provide the names of the permission in the current user
    /// </summary>
    public class UsersPermissionsService : IUsersPermissionsService
    {
        private readonly AuthPermissionsOptions _options;

        public UsersPermissionsService(AuthPermissionsOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// This returns all the permissions in the provided ClaimsPrincipal (or null if no user or permission claim)
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Returns list of permissions in current user, or null if claim not found</returns>
        public List<string> PermissionsFromUser(ClaimsPrincipal user)
        {
            var packedPermissions = user.GetPackedPermissionsFromUser();

            if (packedPermissions == null)
                return null;

            var permissionNames = new List<string>();
            foreach (var permissionChar in packedPermissions)
            {
                var enumName = Enum.GetName(_options.InternalData.EnumPermissionsType, (ushort) permissionChar);
                if (enumName != null)
                    permissionNames.Add( enumName);
            }

            return permissionNames;
        }

    }
}