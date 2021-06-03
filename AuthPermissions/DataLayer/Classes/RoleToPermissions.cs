// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using StatusGeneric;

namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This holds each Roles, which are mapped to Permissions
    /// </summary>
    public class RoleToPermissions
    {
        private RoleToPermissions() { }

        /// <summary>
        /// This creates the Role with its permissions
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="description"></param>
        /// <param name="packedPermissions">The enum values converted to unicode chars</param>
        private RoleToPermissions(string roleName, string description, string packedPermissions)
        {
            RoleName = roleName;
            Update(description, packedPermissions);
        }

        /// <summary>
        /// ShortName of the role
        /// </summary>
        [Key]
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.RoleNameSize)]
        public string RoleName { get; private set; }

        /// <summary>
        /// A human-friendly description of what the Role does
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// This contains the list of permissions as a series of unicode chars
        /// </summary>
        [Required(AllowEmptyStrings = false)] //A role must have at least one role in it
        public string PackedPermissionsInRole { get; private set; }

        public override string ToString()
        {
            var desc = Description == null ? "" : $" (description = {Description})";
            return $"{RoleName}{desc} has {PackedPermissionsInRole.Length} permissions.";
        }

        public static IStatusGeneric<RoleToPermissions> CreateRoleWithPermissions(string roleName, string description,
            string packedPermissions, AuthPermissionsDbContext context)
        {
            var status = new StatusGenericHandler<RoleToPermissions>();
            if (context.Find<RoleToPermissions>(roleName) != null)
            {
                status.AddError("That role already exists");
                return status;
            }

            return status.SetResult(new RoleToPermissions(roleName, description, packedPermissions));
        }

        public void Update(string description, string packedPermissions)
        {
            if (string.IsNullOrEmpty(packedPermissions))
                throw new AuthPermissionsException("There should be at least one permission associated with a role.");

            PackedPermissionsInRole = packedPermissions;
            Description = description;
        }

        public IStatusGeneric DeleteRole(string roleName, bool removeFromUsers,
            AuthPermissionsDbContext context)
        {
            var status = new StatusGenericHandler { Message = "Deleted role successfully." };
            var roleToUpdate = context.Find<RoleToPermissions>(roleName);
            if (roleToUpdate == null)
                return status.AddError("That role doesn't exists");

            var usersWithRoles = context.UserToRoles.Where(x => x.RoleName == roleName).ToList();
            if (usersWithRoles.Any())
            {
                if (!removeFromUsers)
                    return status.AddError($"That role is used by {usersWithRoles.Count} and you didn't ask for them to be updated.");

                context.RemoveRange(usersWithRoles);
                status.Message = $"Removed role from {usersWithRoles.Count} user and then deleted role successfully.";
            }

            context.Remove(roleToUpdate);
            return status;
        }
    }
}