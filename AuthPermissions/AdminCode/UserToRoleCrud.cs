// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This provides the code for admin of users and their roles
    /// </summary>
    public class UserToRoleCrud
    {
        private readonly AuthPermissionsDbContext _context;

        public UserToRoleCrud(AuthPermissionsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get a queryable version of the UserToRoles
        /// </summary>
        /// <returns></returns>
        public IQueryable<UserToRole> UserToRolesQuery()
        {
            return _context.UserToRoles;
        }

        /// <summary>
        /// This returns the names of the roles this user has
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<string>> GetUsersRoleToPermissionsAsync(string userId)
        {
            return await _context.UserToRoles.Where(x => x.UserId == userId)
                .Select(x => x.RoleName).ToListAsync();
        }

        /// <summary>
        /// Add a existing role to the user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> AddRoleToUserAsync(string userId, string roleName, string userName)
        {
            var status = await UserToRole.CreateNewRoleToUserWithChecksAsync(userId, userName, roleName, _context);
            if (status.HasErrors)
                return status;

            await _context.SaveChangesAsync();
            return status;
        }

        /// <summary>
        /// This removes a role from a user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> RemoveRoleFromUserAsync(string userId, string roleName)
        {
            var status = new StatusGenericHandler();
            var userToRole = await _context.UserToRoles
                .SingleOrDefaultAsync( x => x.UserId == userId && x.RoleName == roleName);
            
            if (userToRole == null)
                return status.AddError($"The user does not have the role {roleName}");

            _context.Remove(userToRole);
            await _context.SaveChangesAsync();
            return status;
        }
    }
}