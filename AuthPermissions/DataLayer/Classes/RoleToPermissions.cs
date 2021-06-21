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
    public class RoleToPermissions : INameToShowOnException
    {
        private RoleToPermissions() { }

        /// <summary>
        /// This creates the Role with its permissions
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="description"></param>
        /// <param name="packedPermissions">The enum values converted to unicode chars</param>
        public RoleToPermissions(string roleName, string description, string packedPermissions)
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

        //--------------------------------------------------
        // Exception Error name

        /// <summary>
        /// Used when there is an exception
        /// </summary>
        public string NameToUseForError => RoleName;

        //-------------------------------------------------------------
        //access methods

        public void Update(string description, string packedPermissions)
        {
            if (string.IsNullOrEmpty(packedPermissions))
                throw new AuthPermissionsException("There should be at least one permission associated with a role.");

            PackedPermissionsInRole = packedPermissions;
            Description = description;
        }

        /// <summary>
        /// Delete this RoleToPermission, with checks/delete of UserToRole from users with this role
        /// </summary>
        /// <param name="removeFromUsers">IF true it will delete all UserToRole with this role.
        /// Otherwise return error if there are users with this role</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public IStatusGeneric DeleteRole(bool removeFromUsers, AuthPermissionsDbContext context)
        {
            var status = new StatusGenericHandler { Message = "Deleted role successfully." };

            var usersWithRoles = context.UserToRoles.Where(x => x.RoleName == RoleName).ToList();
            if (usersWithRoles.Any())
            {
                if (!removeFromUsers)
                    return status.AddError($"That role is used by {usersWithRoles.Count} and you didn't ask for them to be updated.");

                context.RemoveRange(usersWithRoles);
                status.Message = $"Removed role from {usersWithRoles.Count} user and then deleted role successfully.";
            }

            context.Remove(this);
            return status;
        }
    }
}