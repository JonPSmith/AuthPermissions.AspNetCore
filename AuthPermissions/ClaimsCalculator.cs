// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Security.Claims;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.PermissionsCode;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions
{
    /// <summary>
    /// This service returns the authPermission claims for an AuthUser
    /// and any extra claims registered using AuthP's AddClaimToUser method when registering AuthP
    /// </summary>
    public class ClaimsCalculator : IClaimsCalculator
    {
        private readonly AuthPermissionsDbContext _context;
        private readonly AuthPermissionsOptions _options;
        private readonly IEnumerable<IClaimsAdder> _claimsAdders;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="claimAdders"></param>
        public ClaimsCalculator(AuthPermissionsDbContext context, 
            AuthPermissionsOptions options,
                IEnumerable<IClaimsAdder> claimAdders)
        {
            _context = context;
            _options = options;
            _claimsAdders = claimAdders;
        }

        /// <summary>
        /// This will return the required AuthP claims, plus any extra claims from registered <see cref="IClaimsAdder"/> methods  
        /// </summary>
        /// <param name="userId"></param>
        /// That's because the JWT Token uses a claim of type "nameid" to hold the ASP.NET Core user's ID</param>
        /// <returns></returns>
        public async Task<List<Claim>> GetClaimsForAuthUserAsync(string userId)
        {
            var result = new List<Claim>();

            var userWithTenant = await _context.AuthUsers.Where(x => x.UserId == userId)
                .Include(x => x.UserTenant)
                .SingleOrDefaultAsync();

            if (userWithTenant == null || userWithTenant.IsDisabled)
                return result;

            var permissions = await CalcPermissionsForUserAsync(userId);
            if (permissions != null) 
                result.Add(new Claim(PermissionConstants.PackedPermissionClaimType, permissions));

            if (_options.TenantType.IsMultiTenant())
                result.AddRange(GetMultiTenantClaims(userWithTenant.UserTenant));

            foreach (var claimsAdder in _claimsAdders)
            {
                var extraClaim = await claimsAdder.AddClaimToUserAsync(userId);
                if (extraClaim != null)
                    result.Add(extraClaim);
            }

            return result;
        }

        //------------------------------------------------------------------------------
        //private methods

        /// <summary>
        /// This is called if the Permissions that a user needs calculating.
        /// It looks at what permissions the user has based on their roles
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>a string containing the packed permissions, or null if no permissions</returns>
        private async Task<string> CalcPermissionsForUserAsync(string userId)
        {
            //This gets all the permissions, with a distinct to remove duplicates
            var permissionsForAllRoles = await _context.UserToRoles
                .Where(x => x.UserId == userId)
                .Select(x => x.Role.PackedPermissionsInRole)
                .ToListAsync();

            if (_options.TenantType.IsMultiTenant())
            {
                //We need to add any RoleTypes.TenantAutoAdd for a tenant user

                var autoAddPermissions = await _context.AuthUsers
                    .Where(x => x.UserId == userId && x.TenantId != null)
                    .SelectMany(x => x.UserTenant.TenantRoles
                        .Where(y => y.RoleType == RoleTypes.TenantAutoAdd)
                        .Select(z => z.PackedPermissionsInRole))
                    .ToListAsync();

                if (autoAddPermissions.Any())
                    permissionsForAllRoles.AddRange(autoAddPermissions);
            }

            if (!permissionsForAllRoles.Any())
                return null;

            //thanks to https://stackoverflow.com/questions/5141863/how-to-get-distinct-characters
            var packedPermissionsForUser = new string(string.Concat(permissionsForAllRoles).Distinct().ToArray());

            return packedPermissionsForUser;
        }

        /// <summary>
        /// This adds the correct claims for a multi-tenant application
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns></returns>
        private List<Claim> GetMultiTenantClaims(Tenant tenant)
        {
            var result = new List<Claim>();

            if (tenant == null)
                return result;

            var dataKey = tenant.GetTenantDataKey();

            result.Add(new Claim(PermissionConstants.DataKeyClaimType, dataKey));

            if (_options.TenantType.IsSharding())
            {
                result.Add(new Claim(PermissionConstants.DatabaseInfoNameType, tenant.DatabaseInfoName));
            }

            return result;
        }
    }
}