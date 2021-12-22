// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This holds each Roles, which are mapped to Permissions
    /// </summary>
    public class RoleToPermissions : INameToShowOnException
    {
#pragma warning disable 649
        // ReSharper disable once CollectionNeverUpdated.Local
        private HashSet<Tenant> _tenants; //filled in by EF Core
#pragma warning restore 649

        private RoleToPermissions() { }

        /// <summary>
        /// This creates the Role with its permissions
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="description"></param>
        /// <param name="packedPermissions">The enum values converted to unicode chars</param>
        /// <param name="roleType">Optional: this sets the type of the Role - only used in multi-tenant apps</param>
        public RoleToPermissions(string roleName, string description, string packedPermissions, RoleTypes roleType = RoleTypes.Normal)
        {
            RoleName = roleName.Trim();
            Update(packedPermissions, description, roleType);
        }

        /// <summary>
        /// Name of the role
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
        /// This contains the RoleType. Only used in multi-tenant application
        /// </summary>
        public RoleTypes RoleType { get; private set; }

        /// <summary>
        /// This contains the list of permissions as a series of unicode chars
        /// </summary>
        [Required(AllowEmptyStrings = false)] //A role must have at least one role in it
        public string PackedPermissionsInRole { get; private set; }

        //----------------------------------------------------
        // Relationships

        /// <summary>
        /// This links a RoleToPermission with a <see cref="RoleType"/> of
        /// <see cref="RoleTypes.TenantAutoAdd"/> or <see cref="RoleTypes.TenantAdminAdd"/>
        /// </summary>
        public IReadOnlyCollection<Tenant> Tenants => _tenants?.ToList();

        //-----------------------------------------------------

        /// <summary>
        /// Useful summary
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// This updates the permissions in a AuthP Role
        /// </summary>
        /// <param name="packedPermissions"></param>
        /// <param name="description"></param>
        /// <param name="roleType"></param>
        public void Update(string packedPermissions, string description = null, RoleTypes roleType = RoleTypes.Normal)
        {
            if (string.IsNullOrEmpty(packedPermissions))
                throw new AuthPermissionsException("There should be at least one permission associated with a role.");

            PackedPermissionsInRole = packedPermissions;
            Description = description?.Trim() ?? Description;
            RoleType = roleType;
        }

    }
}