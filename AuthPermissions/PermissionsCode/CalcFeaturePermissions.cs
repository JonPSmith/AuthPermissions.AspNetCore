// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;

namespace FeatureAuthorize
{
    /// <summary>
    /// This is the code that calculates what feature permissions a user has
    /// </summary>
    public class CalcAllowedPermissions
    {
        private readonly AuthPermissionsDbContext _context;

        public CalcAllowedPermissions(AuthPermissionsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// This is called if the Permissions that a user needs calculating.
        /// It looks at what permissions the user has, and then filters out any permissions
        /// they aren't allowed because they haven't get access to the module that permission is linked to.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>a string containing the packed permissions</returns>
        public async Task<string> CalcPermissionsForUserAsync(string userId)
        {
            //This gets all the permissions, with a distinct to remove duplicates
            var packedPermissionsForUser = (await _context.UserToRoles.Where(x => x.UserId == userId)
                .SelectMany(x => x.Role.PackedPermissionsInRole)
                .Distinct().ToListAsync())
                .ToString();

            //we get the modules this user is allowed to see
            //var userModules = _context.ModulesForUsers.Find(userId)
            //        ?.AllowedPaidForModules ?? PaidForModules.None;
            //Now we remove permissions that are linked to modules that the user has no access to
            //var filteredPermissions =
            //    from permission in permissionsForUser
            //    let moduleAttr = typeof(Permissions).GetMember(permission.ToString())[0]
            //        .GetCustomAttribute<LinkedToModuleAttribute>()
            //    where moduleAttr == null || userModules.HasFlag(moduleAttr.PaidForModule)
            //    select permission;

            return packedPermissionsForUser;
        }
    }
}