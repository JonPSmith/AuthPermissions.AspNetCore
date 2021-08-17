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
        public AuthUsersAdminService(AuthPermissionsDbContext context, IAuthPServiceFactory<ISyncAuthenticationUsers> syncAuthenticationUsersFactory, AuthPermissionsOptions options)
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
                status.AddError($"Could not find the AuthP User with the email of {email}.", nameof(email).CamelToPascal());

            return status.SetResult(authUser);
        }

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
                    if (syncChange.FoundChange == SyncAuthUserChanges.Update)
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
        /// This uses the <see cref="SyncAuthUserWithChange.FoundChange"/> parameter to define what to change
        /// </summary>
        /// <param name="changesToApply"></param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> ApplySyncChangesAsync(IEnumerable<SyncAuthUserWithChange> changesToApply)
        {
            var status = new StatusGenericHandler();

            foreach (var syncChange in changesToApply)
            {
                switch (syncChange.FoundChange)
                {
                    case SyncAuthUserChanges.NoChange:
                        continue;
                    case SyncAuthUserChanges.Add:
                        status.CombineStatuses(await AddUpdateAuthUserAsync(syncChange, false));
                        break;
                    case SyncAuthUserChanges.Update:
                        status.CombineStatuses(await AddUpdateAuthUserAsync(syncChange, true));
                        break;
                    case SyncAuthUserChanges.Remove:
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
            var changeStrings = Enum.GetValues<SyncAuthUserChanges>().ToList()
                .Select(x => $"{x} = {changesToApply.Count(y => y.FoundChange == x)}");
            status.Message = $"Sync successful: {(string.Join(", ", changeStrings))}";

            return status;
        }

        private async Task<IStatusGeneric> AddUpdateAuthUserAsync(SyncAuthUserWithChange newUserData, bool update)
        {
            var status = new StatusGenericHandler();
            var roles = newUserData.RoleNames == null
                ? new List<RoleToPermissions>()
                : await _context.RoleToPermissions.Where(x => newUserData.RoleNames.Contains(x.RoleName))
                .ToListAsync();

            if (roles.Count < (newUserData.RoleNames?.Count ?? 0))
            {
                //Could not find one or more Roles
                var missingRoleNames = newUserData.RoleNames;
                roles.ForEach(x => missingRoleNames.Remove(x.RoleName));

                return status.AddError(
                    $"The following role names were not found: {string.Join(", ", missingRoleNames)}", nameof(SyncAuthUserWithChange.RoleNames));
            }

            Tenant tenant = null;         
            if (newUserData.TenantName != null && newUserData.TenantName != CommonConstants.EmptyTenantName)
            {
                tenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantFullName == newUserData.TenantName);
                if (tenant == null)
                    return status.AddError($"Could not find the tenant {newUserData.TenantName}", nameof(SyncAuthUserWithChange.TenantName));
            }

            //If all ok then we can add/update
            if(!update)
                //Simple add
                _context.Add(new AuthUser(newUserData.UserId, newUserData.Email, newUserData.UserName, roles, tenant));
            else
            {
                var getUserStatus = await FindAuthUserByUserIdAsync(newUserData.UserId);
                if (status.CombineStatuses(getUserStatus).HasErrors)
                    return status;

                getUserStatus.Result.ChangeUserNameAndEmailWithChecks(newUserData.Email, newUserData.UserName); //if same then ignored
                getUserStatus.Result.UpdateUserTenant(tenant);//if same then ignored
                if (newUserData.RoleNames != null &&
                    newUserData.RoleNames.OrderBy(x => x) == getUserStatus.Result.UserRoles.Select(x => x.RoleName).OrderBy(x => x))
                    //The RoleNames were filled in and the roles are different have changed
                    getUserStatus.Result.ReplaceAllRoles(roles);
            }
            return status;
        }

        /// <summary>
        /// This will set the UserName and email properties in the AuthUser
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="userName">new user name</param>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> ChangeUserNameAndEmailAsync(AuthUser authUser, string userName, string email)
        {
            if (authUser == null) throw new ArgumentNullException(nameof(authUser));
            if (string.IsNullOrEmpty(userName))
                throw new AuthPermissionsBadDataException("Cannot be null or an empty string", nameof(userName));
            var status = new StatusGenericHandler { Message = $"Successfully changed the UserName from {authUser.UserName} to {userName}." };

            if (!email.IsValidEmail())
                return status.AddError($"The email '{email}' is not a valid email.", nameof(email).CamelToPascal());

            authUser.ChangeUserNameAndEmailWithChecks(email, userName);
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
        /// This allows you to add or change a tenant to a AuthP User
        /// NOTE: you must have set the <see cref="AuthPermissions.AuthPermissionsOptions.TenantType"/> to a valid tenant type for this to work
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="tenantFullName">The full name of the tenant</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> ChangeTenantToUserAsync(AuthUser authUser, string tenantFullName)
        {
            if (authUser == null) throw new ArgumentNullException(nameof(authUser));
            if (string.IsNullOrEmpty(tenantFullName))
                throw new AuthPermissionsBadDataException("Cannot be null or an empty string", (nameof(tenantFullName)));

            var status = new StatusGenericHandler
            {
                Message = $"Changed the tenant to {tenantFullName} on auth user {authUser.UserName ?? authUser.Email}."
            };

            if (_tenantType == TenantTypes.NotUsingTenants)
                return status.AddError($"You have not configured the {nameof(AuthPermissionsOptions.TenantType)} to use tenants.");

            var tenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantFullName == tenantFullName);
            if (tenant == null)
                return status.AddError($"Could not find the tenant {tenantFullName}", nameof(tenantFullName).CamelToPascal());

            authUser.UpdateUserTenant(tenant);
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

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
    }
}