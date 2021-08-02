// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.PermissionsCode.Internal;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.AdminCode.Services
{
    /// <summary>
    /// This provides CRUD access to the AuthP's Roles
    /// </summary>
    public class AuthRolesAdminService : IAuthRolesAdminService
    {
        private readonly AuthPermissionsDbContext _context;
        private readonly Type _permissionType;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        public AuthRolesAdminService(AuthPermissionsDbContext context, AuthPermissionsOptions options)
        {
            _context = context;
            _permissionType = options.InternalData.EnumPermissionsType;
        }

        /// <summary>
        /// This simply returns a IQueryable of the <see cref="RoleWithPermissionNamesDto"/>.
        /// This contains all the properties in the <see cref="RoleToPermissions"/> class, plus a list of the Permissions names
        /// </summary>
        /// <returns>query on the database</returns>
        public IQueryable<RoleWithPermissionNamesDto> QueryRoleToPermissions()
        {
            return _context.RoleToPermissions.Select(x => new RoleWithPermissionNamesDto
            {
                RoleName = x.RoleName,
                Description = x.Description,
                PackedPermissionsInRole = x.PackedPermissionsInRole,
                PermissionNames = x.PackedPermissionsInRole.ConvertPackedPermissionToNames(_permissionType)
            });
        }

        /// <summary>
        /// This returns a list of permissions with the information from the Display attribute
        /// </summary>
        /// <param name="excludeFilteredPermissions">Optional: If set to true, then filtered permissions are also included.</param>
        /// <param name="groupName">optional: If true  it only returns permissions in a specific group</param>
        /// <returns></returns>
        public List<PermissionDisplay> GetPermissionDisplay(bool excludeFilteredPermissions, string groupName = null)
        {
            var allPermissions = PermissionDisplay
                .GetPermissionsToDisplay(_permissionType, excludeFilteredPermissions);

            return groupName == null
                ? allPermissions
                : allPermissions.Where(x => x.GroupName == groupName).ToList();
        }

        /// <summary>
        /// This returns a query containing all the AuthP users that have the given role name
        /// </summary>
        public IQueryable<AuthUser> QueryUsersUsingThisRole(string roleName)
        {
            return _context.AuthUsers.Where(x => x.UserRoles.Any(y => y.RoleName == roleName));
        }

        /// <summary>
        /// This adds a new RoleToPermissions with the given description and permissions defined by the names 
        /// </summary>
        /// <param name="roleName">Name of the new role (must be unique)</param>
        /// <param name="description">A description to tell you what this role allows the user to use</param>
        /// <param name="permissionNames">a collection of permission names to go into this role</param>
        /// <returns>A status with any errors found</returns>
        public async Task<IStatusGeneric> AddRoleToPermissionsAsync(string roleName, string description, IEnumerable<string> permissionNames)
        {
            var status = new StatusGenericHandler { Message = $"Successfully added the new role {roleName}." };

            var packedPermissions = _permissionType.PackPermissionsNamesWithValidation(permissionNames, 
                x => status.AddError($"The permission name '{x}' isn't a valid name in the {_permissionType.Name} enum."));

            if (status.HasErrors)
                return status;

            if (!packedPermissions.Any())
                return status.AddError("You must provide at least one permission name.");

            _context.Add(new RoleToPermissions(roleName, description, packedPermissions));
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            return status;
        }

        /// <summary>
        /// This updates the role's permission names, and optionally its description
        /// </summary>
        /// <param name="roleName">Name of an existing role</param>
        /// <param name="permissionNames">a collection of permission names to go into this role</param>
        /// <param name="description">Optional: If given then updates the description for this role</param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> UpdateRoleToPermissionsAsync(string roleName, IEnumerable<string> permissionNames, string description = null)
        {
            var status = new StatusGenericHandler { Message = $"Successfully updated the role {roleName}." };
            var existingRolePermission = await _context.RoleToPermissions.SingleOrDefaultAsync(x => x.RoleName == roleName);

            if (existingRolePermission == null)
                return status.AddError($"Could not find a role called {roleName}");

            var packedPermissions = _permissionType.PackPermissionsNamesWithValidation(permissionNames,
                x => status.AddError($"The permission name '{x}' isn't a valid name in the {_permissionType.Name} enum."));

            if (status.HasErrors)
                return status;

            if (!packedPermissions.Any())
                return status.AddError("You must provide at least one permission name.");

            existingRolePermission.Update(packedPermissions, description);
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            return status;
        }

        /// <summary>
        /// This deletes a Role. If that Role is already assigned to AuthP users you must set the removeFromUsers to true
        /// otherwise you will get an error.
        /// </summary>
        /// <param name="roleName">name of role to delete</param>
        /// <param name="removeFromUsers">If false it will fail if any AuthP user have that role.
        /// If true it will delete the role from all the users that have it.</param>
        /// <returns>status</returns>
        public async Task<IStatusGeneric> DeleteRoleAsync(string roleName, bool removeFromUsers)
        {
            var status = new StatusGenericHandler {Message = $"Successfully deleted the role {roleName}."};

            var existingRolePermission =
                await _context.RoleToPermissions.SingleOrDefaultAsync(x => x.RoleName == roleName);

            if (existingRolePermission == null)
                return status.AddError($"Could not find a role called {roleName}");

            var usersWithRoles = _context.UserToRoles.Where(x => x.RoleName == roleName).ToList();
            if (usersWithRoles.Any())
            {
                if (!removeFromUsers)
                    return status.AddError(
                        $"That role is used by {usersWithRoles.Count} and you didn't ask for them to be updated.");

                _context.RemoveRange(usersWithRoles);
                status.Message = $"Successfully deleted the role {roleName} and removed that role from {usersWithRoles.Count} users.";
            }


            _context.Remove(existingRolePermission);
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync());

            return status;
        }
    }
}