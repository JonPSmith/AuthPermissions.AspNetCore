// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using StatusGeneric;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This provides CRUD access to the AuthP's Users
    /// </summary>
    public interface IAuthUsersAdminService
    {
        /// <summary>
        /// This returns a IQueryable of AuthUser, with optional filtering by dataKey and sharding name (useful for tenant admin)
        /// </summary>
        /// <param name="dataKey">optional dataKey. If provided then it only returns AuthUsers that fall within that dataKey</param>
        /// <param name="databaseInfoName">optional sharding name. If provided then it only returns AuthUsers that fall within that dataKey</param>
        /// <returns>query on the database</returns>
        IQueryable<AuthUser> QueryAuthUsers(string dataKey = null, string databaseInfoName = null);

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
        /// This will changes the <see cref="AuthUser.IsDisabled"/> for the user with the given userId
        /// A disabled user causes the <see cref="ClaimsCalculator"/> to not add any AuthP claims to the user on login 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="isDisabled">New setting for the <see cref="AuthUser.IsDisabled"/></param>
        /// <returns>Status containing the AuthUser with UserRoles and UserTenant, or errors</returns>
        Task<IStatusGeneric> UpdateDisabledAsync(string userId, bool isDisabled);

        /// <summary>
        /// This returns a list of all the RoleNames that can be applied to the AuthUser
        /// Doesn't work properly when used in a create, as the user's tenant hasn't be set
        /// </summary>
        /// <param name="userId">UserId of the user you are updating. Only needed in multi-tenant applications </param>
        /// <param name="addNone">Defaults to true, with will add the <see cref="CommonConstants.EmptyItemName"/> at the start.
        /// This is useful for selecting no roles</param>
        /// <returns></returns>
        Task<List<string>> GetRoleNamesForUsersAsync(string userId = null, bool addNone = true);

        /// <summary>
        /// This returns all the tenant full names
        /// </summary>
        /// <returns></returns>
        Task<List<string>> GetAllTenantNamesAsync();

        /// <summary>
        /// This adds a new AuthUse to the database
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email">if not null, then checked to be a valid email</param>
        /// <param name="userName"></param>
        /// <param name="roleNames">The rolenames of this user</param>
        /// <param name="tenantName">If null, then keeps current tenant. If "" will remove a tenant link.
        /// Otherwise the user will be linked to the tenant with that name.</param>
        /// <returns>Status, with created AuthUser</returns>
        Task<IStatusGeneric<AuthUser>> AddNewUserAsync(string userId, string email,
            string userName, List<string> roleNames, string tenantName = null);

        /// <summary>
        /// This update an existing AuthUser. This method is designed so you only have to provide data for the parts you want to update,
        /// i.e. if a parameter is null, then it keeps the original setting. The only odd one out is the tenantName,
        /// where you have to provide the <see cref="CommonConstants.EmptyItemName"/> value to remove the tenant.  
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email">Either provide a email or null. if null, then uses the current user's email</param>
        /// <param name="userName">Either provide a userName or null. if null, then uses the current user's userName</param>
        /// <param name="roleNames">Either a list of rolenames or null. If null, then keeps its current rolenames.</param>
        /// <param name="tenantName">If null, then keeps current tenant. If it is <see cref="CommonConstants.EmptyItemName"/> it will remove a tenant link.
        /// Otherwise the user will be linked to the tenant with that name.</param>
        /// <returns>status</returns>
        Task<IStatusGeneric> UpdateUserAsync(string userId,
            string email = null, string userName = null, List<string> roleNames = null, string tenantName = null);

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
        /// This uses the <see cref="SyncAuthUserWithChange.FoundChangeType"/> parameter to define what to change
        /// </summary>
        /// <param name="changesToApply"></param>
        /// <returns>Status</returns>
        Task<IStatusGeneric> ApplySyncChangesAsync(IEnumerable<SyncAuthUserWithChange> changesToApply);
    }
}