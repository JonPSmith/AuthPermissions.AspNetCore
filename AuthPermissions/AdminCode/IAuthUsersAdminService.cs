// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using StatusGeneric;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This provides CRUD access to the AuthP's Users
    /// </summary>
    public interface IAuthUsersAdminService
    {
        /// <summary>
        /// This returns a IQueryable of AuthUser, with optional filtering by dataKey (useful for tenant admin
        /// </summary>
        /// <param name="dataKey">optional dataKey. If provided then it only returns AuthUsers that fall within that dataKey</param>
        /// <returns>query on the database</returns>
        IQueryable<AuthUser> QueryAuthUsers(string dataKey = null);

        /// <summary>
        /// Finds a AuthUser via its UserId. Returns a status with an error if not found
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>Status containing the AuthUser with UserRoles and UserTenant, or errors</returns>
        Task<IStatusGeneric<AuthUser>> FindAuthUserByUserIdAsync(string userId);

        /// <summary>
        /// Find a AuthUser via its email. Returns a status with an error if not found
        /// </summary>
        /// <param name="email"></param>
        /// <returns>Status containing the AuthUser with UserRoles and UserTenant, or errors</returns>
        Task<IStatusGeneric<AuthUser>> FindAuthUserByEmailAsync(string email);

        /// <summary>
        /// This adds a new AuthUse to the database
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email">if not null, then checked to be a valid email</param>
        /// <param name="userName"></param>
        /// <param name="roleNames">The rolenames of this user - if null then assumes no roles</param>
        /// <param name="tenantName">optional: full name of the tenant</param>
        /// <returns></returns>
        Task<IStatusGeneric> AddNewUserAsync(string userId, string email,
            string userName, List<string> roleNames, string tenantName = null);

        /// <summary>
        /// This update an existing AuthUser
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email">if not null, then checked to be a valid email</param>
        /// <param name="userName"></param>
        /// <param name="roleNames">The rolenames of this user - if null then assumes no roles</param>
        /// <param name="tenantName">optional: full name of the tenant</param>
        /// <returns></returns>
        Task<IStatusGeneric> UpdateUserAsync(string userId, string email,
            string userName, List<string> roleNames, string tenantName = null);

        /// <summary>
        /// This adds a auth role to the auth user
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        Task<IStatusGeneric> AddRoleToUser(AuthUser authUser, string roleName);

        /// <summary>
        /// This removes a auth role from the auth user
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="roleName"></param>
        /// <returns>status</returns>
        Task<IStatusGeneric> RemoveRoleToUser(AuthUser authUser, string roleName);

        /// <summary>
        /// This will delete the AuthUser with the given userId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>status</returns>
        Task<IStatusGeneric> DeleteUserAsync(string userId);

        //---------------------------------------------------------
        //user sync methods

        /// <summary>
        /// This compares the users in the authentication provider against the user's in the AuthP's database.
        /// It creates a list of all the changes (add, update, remove) than need to be applied to the AuthUsers.
        /// This is shown to the admin user to check, and fill in the Roles/Tenant parts for new users
        /// </summary>
        /// <returns>Status, if valid then it contains a list of <see cref="SyncAuthUserWithChange"/>to display</returns>
        Task<List<SyncAuthUserWithChange>> SyncAndShowChangesAsync();

        /// <summary>
        /// This receives a list of <see cref="SyncAuthUserWithChange"/> and applies them to the AuthP database.
        /// This uses the <see cref="SyncAuthUserWithChange.FoundChange"/> parameter to define what to change
        /// </summary>
        /// <param name="changesToApply"></param>
        /// <returns>Status</returns>
        Task<IStatusGeneric> ApplySyncChangesAsync(IEnumerable<SyncAuthUserWithChange> changesToApply);
    }
}