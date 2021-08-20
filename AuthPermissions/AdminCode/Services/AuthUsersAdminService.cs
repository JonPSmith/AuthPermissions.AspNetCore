// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using AuthPermissions.SetupCode.Factories;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.AdminCode.Services
{
    /// <summary>
    /// This provides CRUD access to the AuthP's Users
    /// </summary>
    public class AuthUsersAdminService : IAuthUsersAdminService
    {
        private readonly AuthPermissionsDbContext _context;
        private readonly IAuthPServiceFactory<ISyncAuthenticationUsers> _syncAuthenticationUsersFactory;
        private readonly TenantTypes _tenantType;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="syncAuthenticationUsersFactory">A factory to create an authentication sync provider</param>
        /// <param name="options">auth options</param>
        public AuthUsersAdminService(AuthPermissionsDbContext context,
            IAuthPServiceFactory<ISyncAuthenticationUsers> syncAuthenticationUsersFactory,
            AuthPermissionsOptions options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _syncAuthenticationUsersFactory = syncAuthenticationUsersFactory;
            _tenantType = options.TenantType;
        }

        /// <summary>
        /// This returns a IQueryable of AuthUser, with optional filtering by dataKey (useful for tenant admin
        /// </summary>
        /// <param name="dataKey">optional dataKey. If provided then it only returns AuthUsers that fall within that dataKey</param>
        /// <returns>query on the database</returns>
        public IQueryable<AuthUser> QueryAuthUsers(string dataKey = null)
        {
            return dataKey == null
                ? _context.AuthUsers
                : _context.AuthUsers.Where(x => (x.UserTenant.ParentDataKey ?? "." + x.TenantId).StartsWith(dataKey));
        }

        /// <summary>
        /// Finds a AuthUser via its UserId. Returns a status with an error if not found
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>Status containing the AuthUser with UserRoles and UserTenant, or errors</returns>
        public async Task<IStatusGeneric<AuthUser>> FindAuthUserByUserIdAsync(string userId)
        {
            if (userId == null) throw new ArgumentNullException(nameof(userId));
            var status = new StatusGenericHandler<AuthUser>();

            var authUser = await _context.AuthUsers
                .Include(x => x.UserRoles)
                .Include(x => x.UserTenant)
                .SingleOrDefaultAsync(x => x.UserId == userId);

            if (authUser == null)
                status.AddError("Could not find the AuthP User you asked for.", nameof(userId).CamelToPascal());

            return status.SetResult(authUser);
        }

        /// <summary>
        /// Find a AuthUser via its email. Returns a status with an error if not found
        /// </summary>
        /// <param name="email"></param>
        /// <returns>Status containing the AuthUser with UserRoles and UserTenant, or errors</returns>
        public async Task<IStatusGeneric<AuthUser>> FindAuthUserByEmailAsync(string email)
        {
            if (email == null) throw new ArgumentNullException(nameof(email));
            var status = new StatusGenericHandler<AuthUser>();

            var authUser = await _context.AuthUsers
                .Include(x => x.UserRoles)
                .Include(x => x.UserTenant)
                .SingleOrDefaultAsync(x => x.Email == email);

            if (authUser == null)
                status.AddError($"Could not find the AuthP User with the email of {email}.",
                    nameof(email).CamelToPascal());

            return status.SetResult(authUser);
        }

        /// <summary>
        /// This returns a list of all the RoleNames
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAllRoleNamesAsync()
        {
            return await _context.RoleToPermissions.Select(x => x.RoleName).ToListAsync();
        }

        /// <summary>
        /// This returns all the tenant full names
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAllTenantNamesAsync()
        {
            return await _context.Tenants.Select(x => x.TenantFullName).ToListAsync();
        }

        /// <summary>
        /// This adds a new AuthUse to the database
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email">if not null, then checked to be a valid email</param>
        /// <param name="userName"></param>
        /// <param name="roleNames">The rolenames of this user - if null then assumes no roles</param>
        /// <param name="tenantName">optional: full name of the tenant</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> AddNewUserAsync(string userId, string email,
            string userName, List<string> roleNames, string tenantName = null)
        {
            var status = new StatusGenericHandler
                { Message = $"Successfully added a AuthUser with the name {userName ?? email}" };

            if (email != null && !email.IsValidEmail())
                status.AddError($"The email '{email}' is not a valid email.");

            //Find the tenant
            var foundTenant = string.IsNullOrEmpty(tenantName) || tenantName == CommonConstants.EmptyTenantName
                ? null
                : await _context.Tenants.SingleOrDefaultAsync(x => x.TenantFullName == tenantName);
            if (!string.IsNullOrEmpty(tenantName) && tenantName != CommonConstants.EmptyTenantName && foundTenant == null)
                status.AddError($"A tenant with the name '{tenantName}' wasn't found.");

            //Find the roles
            var foundRoles = roleNames?.Any() == true
                ? await _context.RoleToPermissions
                    .Where(x => roleNames.Contains(x.RoleName))
                    .ToListAsync()
                : new List<RoleToPermissions>();
            if (foundRoles.Count != (roleNames?.Count ?? 0))
                throw new AuthPermissionsBadDataException("One or more role names weren't found in the database.");

            if (status.HasErrors)
                return status;

            var authUser = new AuthUser(userId, email, userName, foundRoles, foundTenant);
            _context.Add(authUser);
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            return status;
        }

        /// <summary>
        /// This update an existing AuthUser
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email">if not null, then checked to be a valid email</param>
        /// <param name="userName"></param>
        /// <param name="roleNames">The rolenames of this user - if null then assumes no roles</param>
        /// <param name="tenantName">optional: full name of the tenant</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> UpdateUserAsync(string userId, string email,
            string userName, List<string> roleNames, string tenantName = null)
        {
            var status = new StatusGenericHandler
            { Message = $"Successfully updated a AuthUser with the name {userName ?? email}" };

            var foundUserStatus = await FindAuthUserByUserIdAsync(userId);
            if (status.CombineStatuses(foundUserStatus).HasErrors)
                return status;

            var authUserToUpdate = foundUserStatus.Result;

            if (email != null && !email.IsValidEmail())
                status.AddError($"The email '{email}' is not a valid email.");

            //Find the tenant
            var foundTenant = string.IsNullOrEmpty(tenantName) || tenantName == CommonConstants.EmptyTenantName
                ? null
                : await _context.Tenants.SingleOrDefaultAsync(x => x.TenantFullName == tenantName);
            if (!string.IsNullOrEmpty(tenantName) && tenantName != CommonConstants.EmptyTenantName && foundTenant == null)
                status.AddError($"A tenant with the name '{tenantName}' wasn't found.");

            var foundRoles = roleNames?.Any() == true
                ? await _context.RoleToPermissions
                    .Where(x => roleNames.Contains(x.RoleName))
                    .ToListAsync()
                : new List<RoleToPermissions>();
            if (foundRoles.Count != (roleNames?.Count ?? 0))
                throw new AuthPermissionsBadDataException("One or more role names weren't found in the database.");

            if (status.HasErrors)
                return status;

            //Now we update the existing AuthUser
            authUserToUpdate.ChangeUserNameAndEmailWithChecks(email, userName);
            authUserToUpdate.UpdateUserTenant(foundTenant);
            if (foundRoles.Any() && authUserToUpdate.UserRoles.Select(x => x.RoleName).OrderBy(x => x) !=
                roleNames.OrderBy(x => x))
                //Different roles, so change
                authUserToUpdate.ReplaceAllRoles(foundRoles);

            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            return status;
        }

        /// <summary>
        /// This adds a auth role to the auth user
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> AddRoleToUser(AuthUser authUser, string roleName)
        {
            if (authUser == null) throw new ArgumentNullException(nameof(authUser));
            if (string.IsNullOrEmpty(roleName))
                throw new AuthPermissionsBadDataException("Cannot be null or an empty string", (nameof(roleName)));
            if (authUser.UserRoles == null)
                throw new AuthPermissionsBadDataException($"The AuthUser's {nameof(AuthUser.UserRoles)} must be loaded", (nameof(authUser)));

            var status = new StatusGenericHandler();

            var role = await _context.RoleToPermissions.SingleOrDefaultAsync(x => x.RoleName == roleName);

            if (role == null)
                return status.AddError($"Could not find the role {roleName}", nameof(roleName).CamelToPascal());

            var added = authUser.AddRoleToUser(role);
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            status.Message = added
                ? $"Successfully added the role {roleName} to auth user {authUser.UserName ?? authUser.Email}."
                : $"The auth user {authUser.UserName ?? authUser.Email} already had the role {roleName}";

            return status;
        }

        /// <summary>
        /// This removes a auth role from the auth user
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="roleName"></param>
        /// <returns>status</returns>
        public async Task<IStatusGeneric> RemoveRoleToUser(AuthUser authUser, string roleName)
        {
            if (authUser == null) throw new ArgumentNullException(nameof(authUser));
            if (string.IsNullOrEmpty(roleName))
                throw new AuthPermissionsBadDataException("Cannot be null or an empty string", (nameof(roleName)));
            if (authUser.UserRoles == null)
                throw new AuthPermissionsBadDataException($"The AuthUser's {nameof(AuthUser.UserRoles)} must be loaded", (nameof(authUser)));

            var status = new StatusGenericHandler();

            var role = await _context.RoleToPermissions.SingleOrDefaultAsync(x => x.RoleName == roleName);

            if (role == null)
                return status.AddError($"Could not find the role {roleName}", nameof(roleName).CamelToPascal());

            var removed = authUser.RemoveRoleFromUser(role);
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            status.Message = removed
                ? $"Successfully removed the role {roleName} to auth user {authUser.UserName ?? authUser.Email}."
                : $"The auth user {authUser.UserName ?? authUser.Email} didn't have the role {roleName}";

            return status;
        }

        /// <summary>
        /// This will delete the AuthUser with the given userId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>status</returns>
        public async Task<IStatusGeneric> DeleteUserAsync(string userId)
        {
            var status = new StatusGenericHandler();

            var authUser = await _context.AuthUsers.SingleOrDefaultAsync(x => x.UserId == userId);

            if (authUser == null)
                return status.AddError("Could not find the user you were looking for.", nameof(userId).CamelToPascal());

            _context.Remove(authUser);
            status.CombineStatuses( await _context.SaveChangesWithChecksAsync());

            status.Message = $"Successfully deleted the user {authUser.UserName ?? authUser.Email}.";

            return status;
        }

        //----------------------------------------------------------------------------
        // sync code

        /// <summary>
        /// This compares the users in the authentication provider against the user's in the AuthP's database.
        /// It creates a list of all the changes (add, update, remove) than need to be applied to the AuthUsers.
        /// This is shown to the admin user to check, and fill in the Roles/Tenant parts for new users
        /// </summary>
        /// <returns>Status, if valid then it contains a list of <see cref="SyncAuthUserWithChange"/>to display</returns>
        public async Task<List<SyncAuthUserWithChange>> SyncAndShowChangesAsync()
        {
            //This throws an exception if the developer hasn't configured the service
            var syncAuthenticationUsers = _syncAuthenticationUsersFactory.GetService();

            var authenticationUsers = await syncAuthenticationUsers.GetAllActiveUserInfoAsync();
            var authUserDictionary = await _context.AuthUsers
                .Include(x => x.UserRoles)
                .Include(x => x.UserTenant)
                .ToDictionaryAsync(x => x.UserId);

            var result = new List<SyncAuthUserWithChange>();
            foreach (var authenticationUser in authenticationUsers)
            {
                if (authUserDictionary.TryGetValue(authenticationUser.UserId, out var authUser))
                {
                    //check if its a change or not
                    var syncChange = new SyncAuthUserWithChange(authenticationUser, authUser);
                    if (syncChange.FoundChangeType == SyncAuthUserChangeTypes.Update)
                        //The two are different so add to the result
                        result.Add(syncChange);
                    //Removed the authUser as has been handled
                    authUserDictionary.Remove(authenticationUser.UserId);
                }
                else
                {
                    //A new AuthUser should be created
                    result.Add(new SyncAuthUserWithChange(authenticationUser, null));
                }
            }

            //All the authUsers still in the authUserDictionary are not in the authenticationUsers, so mark as remove
            result.AddRange(authUserDictionary.Values.Select(x => new SyncAuthUserWithChange(null, x)));

            return result;
        }

        /// <summary>
        /// This receives a list of <see cref="SyncAuthUserWithChange"/> and applies them to the AuthP database.
        /// This uses the <see cref="SyncAuthUserWithChange.FoundChangeType"/> parameter to define what to change
        /// </summary>
        /// <param name="changesToApply"></param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> ApplySyncChangesAsync(IEnumerable<SyncAuthUserWithChange> changesToApply)
        {
            var status = new StatusGenericHandler();

            foreach (var syncChange in changesToApply)
            {
                switch (syncChange.FoundChangeType)
                {
                    case SyncAuthUserChangeTypes.NoChange:
                        continue;
                    case SyncAuthUserChangeTypes.Create:
                        status.CombineStatuses(await AddNewUserAsync(syncChange.UserId, syncChange.Email,
                            syncChange.UserName, syncChange.RoleNames, syncChange.TenantName));
                        break;
                    case SyncAuthUserChangeTypes.Update:
                        status.CombineStatuses(await UpdateUserAsync(syncChange.UserId, syncChange.Email,
                            syncChange.UserName, syncChange.RoleNames, syncChange.TenantName));
                        break;
                    case SyncAuthUserChangeTypes.Delete:
                        var authUserStatus = await FindAuthUserByUserIdAsync(syncChange.UserId);
                        if (status.CombineStatuses(authUserStatus).HasErrors)
                            return status;

                        _context.Remove(authUserStatus.Result);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());
            //Build useful summary
            var changeStrings = Enum.GetValues<SyncAuthUserChangeTypes>().ToList()
                .Select(x => $"{x} = {changesToApply.Count(y => y.FoundChangeType == x)}");
            status.Message = $"Sync successful: {(string.Join(", ", changeStrings))}";

            return status;
        }

    }
}