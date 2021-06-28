// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;

namespace AuthPermissions.PermissionsCode
{
    public interface IUsersPermissionsService
    {
        /// <summary>
        /// This returns all the permissions in the provided ClaimsPrincipal (or empty list if no user or permission name)
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        List<string> PermissionsFromClaims(ClaimsPrincipal user);
    }
}