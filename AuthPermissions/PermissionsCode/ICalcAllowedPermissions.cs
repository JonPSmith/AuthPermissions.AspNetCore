// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace AuthPermissions.PermissionsCode
{
    public interface ICalcAllowedPermissions
    {
        /// <summary>
        /// This is called if the Permissions that a user needs calculating.
        /// It looks at what permissions the user has based on their roles
        /// FUTURE FEATURE: needs upgrading if TenantId is to change the user's roles.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>a string containing the packed permissions</returns>
        Task<string> CalcPermissionsForUserAsync(string userId);
    }
}