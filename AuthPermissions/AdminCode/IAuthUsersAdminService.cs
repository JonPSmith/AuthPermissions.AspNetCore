// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using StatusGeneric;

namespace AuthPermissions.AdminCode
{
    public interface IAuthUsersAdminService
    {
        /// <summary>
        /// This simply returns a IQueryable of AuthUser
        /// </summary>
        /// <returns>query on the database</returns>
        IQueryable<AuthUser> QueryAuthUsers();

        /// <summary>
        /// Find a AuthUser via its UserId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>AuthUser with UserRoles and UserTenant</returns>
        Task<AuthUser> FindAuthUserByUserIdAsync(string userId);

        /// <summary>
        /// Find a AuthUser via its email
        /// </summary>
        /// <param name="email"></param>
        /// <returns>AuthUser with UserRoles and UserTenant</returns>
        Task<AuthUser> FindAuthUserByEmailAsync(string email);

        /// <summary>
        /// This adds a new Auth User with its Auth roles
        /// </summary>
        /// <param name="userId">This comes from the authentication provider (must be unique)</param>
        /// <param name="email">The email for this user (must be unique)</param>
        /// <param name="userName">Optional user name</param>
        /// <param name="roleNames">List of names of auth roles</param>
        /// <returns>Status</returns>
        Task<IStatusGeneric> AddNewUserWithRolesAsync(string userId, string email, string userName, List<string> roleNames);

        /// <summary>
        /// This will set the UserName property in the AuthUser
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="userName">new user name</param>
        /// <returns></returns>
        Task<IStatusGeneric> ChangeUserNameAsync(AuthUser authUser, string userName);

        /// <summary>
        /// This will set the Email property in the AuthUser
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="email">new user name</param>
        /// <returns></returns>
        Task<IStatusGeneric> ChangeEmailAsync(AuthUser authUser, string email);

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
        /// This allows you to add or change a tenant to a Auth User
        /// NOTE: you must have set the <see cref="AuthPermissionsOptions.TenantType"/> to a valid tenant type for this to work
        /// </summary>
        /// <param name="authUser"></param>
        /// <param name="tenantFullName">The full name of the tenant</param>
        /// <returns></returns>
        Task<IStatusGeneric> ChangeTenantToUserAsync(AuthUser authUser, string tenantFullName);
    }
}