// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.DataKeyCode
{
    /// <summary>
    /// This provides the code to get the multi-tenant data key
    /// </summary>
    public class CalcDataKey : ICalcDataKey
    {
        private readonly AuthPermissionsDbContext _context;
        private readonly IAuthPermissionsOptions _options;

        /// <summary>
        /// Get the AuthPermissionsDbContext and options
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        public CalcDataKey(AuthPermissionsDbContext context, IAuthPermissionsOptions options)
        {
            _context = context;
            _options = options;
        }

        /// <summary>
        /// This return the multi-tenant data key.
        /// It assumes that 
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public async Task<string> GetDataKeyAsync(string userid)
        {
            if (_options.TenantType == TenantTypes.NotUsingTenants)
                return null;

            var userWithTenant = await _context.Users.Include(x => x.UserTenant)
                .SingleOrDefaultAsync(x => x.UserId == userid);
            if (userWithTenant == null)
                throw new AuthPermissionsException(
                    $"The user {userWithTenant.UserName} doesn't have a tenant linked it i.");

            return userWithTenant.UserTenant.TenantDataKey;
        }
    }
}