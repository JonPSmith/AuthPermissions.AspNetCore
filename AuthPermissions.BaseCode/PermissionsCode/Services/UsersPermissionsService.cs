// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.BaseCode.CommonCode;

namespace AuthPermissions.BaseCode.PermissionsCode.Services
{
    /// <summary>
    /// This will provide the names of the permission in the current user
    /// </summary>
    public class UsersPermissionsService : IUsersPermissionsService
    {
        private readonly AuthPermissionsOptions _options;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="options"></param>
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

            return packedPermissions.ConvertPackedPermissionToNames(_options.InternalData.EnumPermissionsType);
        }
    }
}