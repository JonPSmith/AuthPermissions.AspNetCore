// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using StatusGeneric;

namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This is a one-to-many relationship between the User (represented by the UserId) and their Roles (represented by RoleToPermissions)
    /// </summary>
    public class UserToRole : TenantBase
    {
        private UserToRole() {} //Needed by EF Core

        public UserToRole(string userId, string userName, RoleToPermissions role, int tenantId = default)
        {
            UserId = userId;
            UserName = userName;
            Role = role;
            TenantId = tenantId;
        }

        //I use a composite key for this table: combination of UserId, TenantId and RoleName
        //This means:
        //a) a user can have different roles in another tenant (null means same roles for all tenants)
        //b) It ensures a user only links to a role once (per tenant) 

        //That has to be defined by EF Core's fluent API
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)] 
        public string UserId { get; private set; }

        //Contains a name to help work out who the user is
        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get; private set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.RoleNameSize)]
        public string RoleName { get; private set; }

        [ForeignKey(nameof(RoleName))]
        public RoleToPermissions Role { get; private set; }

        public override string ToString()
        {
            return $"User {UserName} has role {RoleName}";
        }


        public static IStatusGeneric<UserToRole> AddRoleToUser(string userId, string userName, string roleName, 
            AuthPermissionsDbContext context, int tenantId = default)
        {
            if (roleName == null) throw new ArgumentNullException(nameof(roleName));

            var status = new StatusGenericHandler<UserToRole>();
            if (context.Find<UserToRole>(userId, tenantId, roleName) != null)
            {
                status.AddError($"The user already has the Role '{roleName}'.");
                return status;
            }
            var roleToAdd = context.Find<RoleToPermissions>(roleName);
            if (roleToAdd == null)
            {
                status.AddError($"I could not find the Role '{roleName}'.");
                return status;
            }

            return status.SetResult(new UserToRole(userId, userName, roleToAdd, tenantId));
        }
    }
}