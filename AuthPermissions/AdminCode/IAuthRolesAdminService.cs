// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.PermissionsCode;
using StatusGeneric;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This is the interface for the AuthP's Roles admin 
    /// </summary>
    public interface IAuthRolesAdminService
    {
        /// <summary>
        /// This returns a IQueryable of the <see cref="RoleWithPermissionNamesDto"/>.
        /// This contains all the properties in the <see cref="RoleToPermissions"/> class, plus a list of the Permissions names
        /// </summary>
        /// <returns>query on the database</returns>
        IQueryable<RoleWithPermissionNamesDto> QueryRoleToPermissions();

        /// <summary>
        /// This returns a list of permissions with the information from the Display attribute
        /// </summary>
        /// <param name="excludeFilteredPermissions">Optional: If set to true, then filtered permissions are also included.</param>
        /// <param name="groupName">optional: If true it only returns permissions in a specific group</param>
        /// <returns></returns>
        List<PermissionDisplay> GetPermissionDisplay(bool excludeFilteredPermissions, string groupName = null);

        /// <summary>
        /// This returns a query containing all the Auth users that have the given role name
        /// </summary>
        IQueryable<AuthUser> QueryUsersUsingThisRole(string roleName);

        /// <summary>
        /// This creates a new RoleToPermissions with the given description and permissions defined by the names 
        /// </summary>
        /// <param name="roleName">Name of the new role (must be unique)</param>
        /// <param name="permissionNames">a collection of permission names to go into this role</param>
        /// <param name="description">An optional description to tell you what this role allows the user to use</param>
        /// <returns>A status with any errors found</returns>
        Task<IStatusGeneric> CreateRoleToPermissionsAsync(string roleName, IEnumerable<string> permissionNames,
            string description = null);

        /// <summary>
        /// This updates the role's permission names, and optionally its description
        /// </summary>
        /// <param name="roleName">Name of an existing role</param>
        /// <param name="permissionNames">a collection of permission names to go into this role</param>
        /// <param name="description">Optional: If given then updates the description for this role</param>
        /// <returns>Status</returns>
        Task<IStatusGeneric> UpdateRoleToPermissionsAsync(string roleName, IEnumerable<string> permissionNames, string description = null);

        /// <summary>
        /// This deletes a Role. If that Role is already assigned to Auth  users you must set the removeFromUsers to true
        /// otherwise you will get an error.
        /// </summary>
        /// <param name="roleName">name of role to delete</param>
        /// <param name="removeFromUsers">If false it will fail if any Auth user have that role.
        /// If true it will delete the role from all the users that have it.</param>
        /// <returns>status</returns>
        Task<IStatusGeneric> DeleteRoleAsync(string roleName, bool removeFromUsers);
    }
}