// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This is a one-to-many relationship between the User (represented by the UserId) and their Roles (represented by RoleToPermissions)
    /// </summary>
    public class UserToRole
    {
        private UserToRole()
        {
        } //Needed by EF Core


        /// <summary>
        /// Create a UserToRole - only used by AuthUser class
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="role"></param>
        internal UserToRole(string userId, RoleToPermissions role)
        {
            UserId = userId;
            Role = role;
        }

        /// <summary>
        /// The user Id
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)]
        public string UserId { get; private set; }

        /// <summary>
        /// The RoleName is part of the key, which ensure that a user only has a role once
        /// It is also a foreign key for the RoleToPermissions
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.RoleNameSize)]
        public string RoleName { get; private set; }

        /// <summary>
        /// Link to the RoleToPermissions
        /// </summary>
        [ForeignKey(nameof(RoleName))] 
        public RoleToPermissions Role { get; private set; }


    }

}