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
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.AdminCode.Services
{
    /// <summary>
    /// This provides CRUD access to the Auth's Users
    /// </summary>
    public class AuthUsersAdminService
    {
        private readonly AuthPermissionsDbContext _context;
        private readonly TenantTypes _tenantType;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        public AuthUsersAdminService(AuthPermissionsDbContext context, IAuthPermissionsOptions options)
        {
            _context = context;
            _tenantType = options.TenantType;
        }

        /// <summary>
        /// This simply returns a IQueryable of AuthUser
        /// </summary>
        /// <returns>query on the database</returns>
        public IQueryable<AuthUser> QueryAuthUsers()
        {
            return _context.AuthUsers;
        }

        /// <summary>
        /// Find a AuthUser via its UserId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>AuthUser with UserRoles and UserTenant</returns>
        public async Task<AuthUser> FindAuthUserByUserIdAsync(string userId)
        {
            return await _context.AuthUsers
                .Include(x => x.UserRoles)
                .Include(x => x.UserTenant)
                .SingleOrDefaultAsync(x => x.UserId == userId);
        }

        /// <summary>
        /// Find a AuthUser via its email
        /// </summary>
        /// <param name="email"></param>
        /// <returns>AuthUser with UserRoles and UserTenant</returns>
        public async Task<AuthUser> FindAuthUserByEmailAsync(string email)
        {
            return await _context.AuthUsers
                .Include(x => x.UserRoles)
                .Include(x => x.UserTenant)
                .SingleOrDefaultAsync(x => x.Email == email);
        }

        /// <summary>
        /// This adds a new Auth User with its Auth roles
        /// </summary>
        /// <param name="userId">This comes from the authentication provider (must be unique)</param>
        /// <param name="email">The email for this user (must be unique)</param>
        /// <param name="userName">Optional user name</param>
        /// <param name="roleNames">List of names of auth roles</param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> AddNewUserWithRolesAsync(string userId, string email, string userName, List<string> roleNames)
        {
            if (string.IsNullOrEmpty(userId)) 
                throw new AuthPermissionsBadDataException("Cannot be null or an empty string", nameof(userId));
            if (string.IsNullOrEmpty(email)) 
                throw new AuthPermissionsBadDataException("Cannot be null or an empty string", (nameof(email)));
            if (roleNames == null) throw new ArgumentNullException(nameof(roleNames));

            var status = new StatusGenericHandler { Message = $"Successfully added a new auth user {userName ?? email}." };

            var roles = await _context.RoleToPermissions.Where(x => roleNames.Contains(x.RoleName))
                .ToListAsync();

            if (roles.Count < roleNames.Count)
            {
                //Could not find one or more Roles
                var missingRoleNames = roleNames;
                roles.ForEach(x => missingRoleNames.Remove(x.RoleName));

                return status.AddError(
                    $"The following role names were not found: {string.Join(", ", missingRoleNames)}");
            }

            _context.Add(new AuthUser(userId, email, userName, roles));
            status.CombineStatuses(await _context.SaveChangesWithUniqueCheckAsync());

            return status;
        }


        /// <summary>
        /// This will set the UserName property in the AuthUser
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="userName">new user name</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> ChangeUserNameAsync(AuthUser authUser, string userName)
        {
            if (authUser == null) throw new ArgumentNullException(nameof(authUser));
            if (string.IsNullOrEmpty(userName))
                throw new AuthPermissionsBadDataException("Cannot be null or an empty string", nameof(userName));

            var status = new StatusGenericHandler { Message = $"Successfully changed the UserName from {authUser.UserName} to {userName}." };
            authUser.ChangeUserName(userName);
            status.CombineStatuses(await _context.SaveChangesWithUniqueCheckAsync());

            return status;
        }

        /// <summary>
        /// This will set the Email property in the AuthUser
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="email">new user name</param>
        /// <returns></returns>
        public async Task<IStatusGeneric> ChangeEmailAsync(AuthUser authUser, string email)
        {
            if (authUser == null) throw new ArgumentNullException(nameof(authUser));
            if (string.IsNullOrEmpty(email))
                throw new AuthPermissionsBadDataException("Cannot be null or an empty string", nameof(email));

            var status = new StatusGenericHandler { Message = $"Successfully changed the email from {authUser.Email} to {email}."};

            if (!email.IsValidEmail())
                return status.AddError($"The email '{email}' is not a valid email.");

            authUser.ChangeEmail(email);
            status.CombineStatuses(await _context.SaveChangesWithUniqueCheckAsync());

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
                return status.AddError($"Could not find the role {roleName}");

            var added = authUser.AddRoleToUser(role);
            status.CombineStatuses(await _context.SaveChangesWithUniqueCheckAsync());

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
                return status.AddError($"Could not find the role {roleName}");

            var removed = authUser.RemoveRoleFromUser(role);
            status.CombineStatuses(await _context.SaveChangesWithUniqueCheckAsync());

            status.Message = removed
                ? $"Successfully removed the role {roleName} to auth user {authUser.UserName ?? authUser.Email}."
                : $"The auth user {authUser.UserName ?? authUser.Email} didn't have the role {roleName}";

            return status;
        }

        /// <summary>
        /// This allows you to add or change a tenant to a Auth User
        /// NOTE: you must have set the <see cref="AuthPermissionsOptions.TenantType"/> to a valid tenant type for this to work
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

            var tenant = await _context.Tenants.SingleOrDefaultAsync(x => x.TenantName == tenantFullName);
            if (tenant == null)
                return status.AddError($"Could not find the tenant {tenantFullName}");

            authUser.UpdateUserTenant(tenant);
            status.CombineStatuses(await _context.SaveChangesWithUniqueCheckAsync());

            return status;
        }


    }
}