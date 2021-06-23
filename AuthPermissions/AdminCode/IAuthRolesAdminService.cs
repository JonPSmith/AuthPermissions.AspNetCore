// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using StatusGeneric;

namespace AuthPermissions.AdminCode
{
    public interface IAuthRolesAdminService
    {
        /// <summary>
        /// This simply returns a IQueryable of RoleToPermissions
        /// </summary>
        /// <returns>query on the database</returns>
        IQueryable<RoleToPermissions> QueryRoleToPermissions();

        /// <summary>
        /// This returns a query containing all the Auth users that have the given role name
        /// </summary>
        IQueryable<AuthUser> QueryUsersUsingThisRole(string roleName);

        /// <summary>
        /// This adds a new RoleToPermissions with the given description and permissions defined by the names 
        /// </summary>
        /// <param name="roleName">Name of the new role (must be unique)</param>
        /// <param name="description">A description to tell you what this role allows the user to use</param>
        /// <param name="permissionNames">a collection of permission names to go into this role</param>
        /// <returns>A status with any errors found</returns>
        Task<IStatusGeneric> AddRoleToPermissionsAsync(string roleName, string description, IEnumerable<string> permissionNames);

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