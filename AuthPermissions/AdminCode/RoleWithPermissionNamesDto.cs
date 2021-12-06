// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// Used in <see cref="IAuthRolesAdminService"/> to return a Role with the permission names 
    /// </summary>
    public class RoleWithPermissionNamesDto
    {
        /// <summary>
        /// Name of the Role (unique)
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.RoleNameSize)]
        public string RoleName { get; set; }

        /// <summary>
        /// A human-friendly description of what the Role does
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// This returns the type of Role this is
        /// </summary>
        public RoleTypes RoleType { get; set; }

        /// <summary>
        /// This contains the list of permissions as a series of unicode chars
        /// </summary>
        [Required(AllowEmptyStrings = false)] //A role must have at least one role in it
        public string PackedPermissionsInRole { get; set; }

        /// <summary>
        /// This contains a list of all Permission names in the role
        /// </summary>
        public List<string> PermissionNames { get; set; }
    }
}