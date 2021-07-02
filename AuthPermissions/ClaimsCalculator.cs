// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions
{
    /// <summary>
    /// This service returns the authPermission claims for an AuthUser
    /// </summary>
    public class ClaimsCalculator : IClaimsCalculator
    {
        private readonly AuthPermissionsOptions _options;
        private readonly AuthPermissionsDbContext _context;

        public ClaimsCalculator(AuthPermissionsDbContext context, AuthPermissionsOptions options)
        {
            _context = context;
            _options = options;
        }

        /// <summary>
        /// This will return the 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<Claim>> GetClaimsForAuthUser(string userId)
        {
            var result = new List<Claim>();

            var permissions = await CalcPermissionsForUserAsync(userId);
            if (permissions != null)
                result.Add(new Claim(PermissionConstants.PackedPermissionClaimType, permissions));

            var dataKey = await GetDataKeyAsync(userId);
            if (dataKey != null)
                result.Add(new Claim(PermissionConstants.DataKeyClaimType, dataKey));
            

            return result;
        }

        //------------------------------------------------------------------------------
        //private methods

        /// <summary>
        /// This is called if the Permissions that a user needs calculating.
        /// It looks at what permissions the user has based on their roles
        /// FUTURE FEATURE: needs upgrading if TenantId is to change the user's roles.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>a string containing the packed permissions, or null if no permissions</returns>
        private async Task<string> CalcPermissionsForUserAsync(string userId)
        {
            //This gets all the permissions, with a distinct to remove duplicates
            var permissionsForAllRoles = (await _context.UserToRoles.Where(x => x.UserId == userId)
                .Select(x => x.Role.PackedPermissionsInRole)
                .ToListAsync());

            if (!permissionsForAllRoles.Any())
                return null;

            //thanks to https://stackoverflow.com/questions/5141863/how-to-get-distinct-characters
            var packedPermissionsForUser = new string(string.Concat(permissionsForAllRoles).Distinct().ToArray());

            return packedPermissionsForUser;
        }

        /// <summary>
        /// This return the multi-tenant data key if one is found
        /// </summary>
        /// <param name="userid"></param>
        /// <returns>Returns the dataKey, or null if a) tenant isn't turned on, or b) the user doesn't have a tenant</returns>
        private async Task<string> GetDataKeyAsync(string userid)
        {
            if (_options.TenantType == TenantTypes.NotUsingTenants)
                return null;

            var userWithTenant = await _context.AuthUsers.Include(x => x.UserTenant)
                .SingleOrDefaultAsync(x => x.UserId == userid);

            return userWithTenant?.UserTenant?.GetTenantDataKey();
        }
    }
}