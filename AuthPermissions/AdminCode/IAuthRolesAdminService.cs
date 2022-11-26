// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.PermissionsCode;
using StatusGeneric;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This is the interface for the AuthP's Roles admin 
    /// </summary>
    public interface IAuthRolesAdminService
    {
        /// <summary>
        /// This returns a IQueryable of the <see cref="RoleWithPermissionNamesDto"/> that the user allowed to see
        /// This contains all the properties in the <see cref="RoleToPermissions"/> class, plus a list of the Permissions names
        /// </summary>
        /// <param name="currentUserId">If your application uses the Multi-Tenant code you must provide the current userId (or null if not logged in)</param>
        /// <returns>query on the database</returns>
        IQueryable<RoleWithPermissionNamesDto> QueryRoleToPermissions(string currentUserId = null);

        /// <summary>
        /// This returns a list of permissions with the information from the Display attribute
        /// NOTE: This should not be called by a user that has a tenant, but this isn't checked
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
        /// This returns a query containing all the Tenants that have given role name
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public IQueryable<Tenant> QueryTenantsUsingThisRole(string roleName);

        /// <summary>
        /// This creates a new RoleToPermissions with the given description and permissions defined by the names
        /// NOTE: This should not be called by a user that has a tenant, but this isn't checked
        /// </summary>
        /// <param name="roleName">Name of the new role (must be unique)</param>
        /// <param name="permissionNames">a collection of permission names to go into this role</param>
        /// <param name="description">The description to tell you what this role allows the user to use - can be null</param>
        /// <param name="roleType">Optional: defaults to <see cref="RoleTypes.Normal"/></param>
        /// <returns>A status with any errors found</returns>
        Task<IStatusGeneric> CreateRoleToPermissionsAsync(string roleName, IEnumerable<string> permissionNames,
            string description, RoleTypes roleType = RoleTypes.Normal);

        /// <summary>
        /// This updates the role's permission names, and optionally its description
        /// if the new permissions contain an advanced permission
        /// </summary>
        /// <param name="roleName">Name of an existing role</param>
        /// <param name="permissionNames">a collection of permission names to go into this role</param>
        /// <param name="description">Optional: If given then updates the description for this role</param>
        /// <param name="roleType">Optional: defaults to <see cref="RoleTypes.Normal"/>.
        /// NOTE: the roleType is changed to <see cref="RoleTypes.HiddenFromTenant"/> if advanced permissions are found</param>
        /// <returns>Status</returns>
        Task<IStatusGeneric> UpdateRoleToPermissionsAsync(string roleName,
            IEnumerable<string> permissionNames,
            string description, RoleTypes roleType = RoleTypes.Normal);

        /// <summary>
        /// This deletes a Role. If that Role is already assigned to Auth users you must set the removeFromUsers to true
        /// otherwise you will get an error.
        /// NOTE: This should not be called by a user that has a tenant, but this isn't checked
        /// </summary>
        /// <param name="roleName">name of role to delete</param>
        /// <param name="removeFromUsers">If false it will fail if any Auth user have that role.
        ///     If true it will delete the role from all the users that have it.</param>
        /// <returns>status</returns>
        Task<IStatusGeneric> DeleteRoleAsync(string roleName, bool removeFromUsers);
    }
}