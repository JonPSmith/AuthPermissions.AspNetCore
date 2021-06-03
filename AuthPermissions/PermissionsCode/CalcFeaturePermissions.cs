// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.PermissionsCode
{
    /// <summary>
    /// This is the code that calculates what feature permissions a user has
    /// </summary>
    public class CalcAllowedPermissions : ICalcAllowedPermissions
    {
        private readonly AuthPermissionsDbContext _context;

        public CalcAllowedPermissions(AuthPermissionsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// This is called if the Permissions that a user needs calculating.
        /// It looks at what permissions the user has based on their roles
        /// FUTURE FEATURE: needs upgrading if TenantId is to change the user's roles.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>a string containing the packed permissions</returns>
        public async Task<string> CalcPermissionsForUserAsync(string userId)
        {
            //This gets all the permissions, with a distinct to remove duplicates
            var permissionsForAllRoles = (await _context.UserToRoles.Where(x => x.UserId == userId)
                .Select(x => x.Role.PackedPermissionsInRole)
                .ToListAsync());

            //thanks to https://stackoverflow.com/questions/5141863/how-to-get-distinct-characters
            var packedPermissionsForUser = new string(string.Concat(permissionsForAllRoles).Distinct().ToArray());

            return packedPermissionsForUser;
        }
    }
}