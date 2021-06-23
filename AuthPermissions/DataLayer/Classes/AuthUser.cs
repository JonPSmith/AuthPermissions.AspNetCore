// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This defines a simple user (UserId, email and username) which will hold the roles and tenant data
    /// for this user.
    /// </summary>
    public class AuthUser : INameToShowOnException
    {
        private HashSet<UserToRole> _userRoles;

        private AuthUser() {} //Needed for EF Core

        /// <summary>
        /// Define a user with there default roles
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email"></param>
        /// <param name="userName"></param>
        /// <param name="roles"></param>
        /// <param name="userTenant"></param>
        public AuthUser(string userId, string email, string userName, IEnumerable<RoleToPermissions> roles, Tenant userTenant = null)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            UserName = userName ?? Email;
            _userRoles = new HashSet<UserToRole>(roles.Select(x => new UserToRole(userId, x)));
            UserTenant = userTenant;
        }

        /// <summary>
        /// The user's Id is its primary key
        /// </summary>
        [Key]
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)]
        public string UserId { get; private set; }

        /// <summary>
        /// Contains the Email, which is used for lookup
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.EmailSize)]
        public string Email { get; private set; }

        /// <summary>
        /// Contains a name to help work out who the user is
        /// </summary>
        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get; private set; }

        //-------------------------------------------------
        //relationships

        /// <summary>
        /// The roles linked to this user
        /// </summary>
        public IReadOnlyCollection<UserToRole> UserRoles => _userRoles?.ToList();

        /// <summary>
        /// foreign key for multi-tenant systems (optional)
        /// </summary>
        public int? TenantId { get; private set; }

        /// <summary>
        /// Tenant for multi-tenant systems
        /// </summary>
        [ForeignKey(nameof(TenantId))]
        public Tenant UserTenant { get; private set; }

        //--------------------------------------------------
        // Exception Error name

        /// <summary>
        /// Used when there is an exception
        /// </summary>
        public string NameToUseForError => UserName ?? Email;

        /// <summary>
        /// Summary of AuthUser
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var tenantString = TenantId == null ? "" 
                : (UserTenant == null ? ", has an tenant" : $", linked to {UserTenant.TenantName}");
            var rolesString = _userRoles == null ? "" : $", roles = {string.Join(", ", _userRoles.Select(x => x.RoleName))}";
            return $"UserName = {UserName}, UserId = {UserId}{rolesString}{tenantString}.";
        }

        //--------------------------------------------------
        // Access methods

        /// <summary>
        /// Adds a RoleToPermissions to the user
        /// </summary>
        /// <param name="role"></param>
        /// <returns>true if added. False if already there</returns>
        public bool AddRoleToUser(RoleToPermissions role)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));

            if (_userRoles.Any(x => x.RoleName == role.RoleName))
                return false;

            _userRoles.Add(new UserToRole(UserId, role));
            return true;
        }

        /// <summary>
        /// This removes a RoleToPermissions from a user
        /// </summary>
        /// <param name="role"></param>
        /// <returns>true if role was found and removed</returns>
        public bool RemoveRoleFromUser(RoleToPermissions role)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            var foundUserToRole = _userRoles.SingleOrDefault(x => x.RoleName == role.RoleName);
            return _userRoles.Remove(foundUserToRole);
        }

        /// <summary>
        /// This updates a tenant.
        /// NOTE: A tenant is only valid if the <see cref="AuthPermissionsOptions.TenantType"/> has been set 
        /// </summary>
        /// <param name="tenant"></param>
        public void UpdateUserTenant(Tenant tenant)
        {
            UserTenant = tenant;
        }

        public void ChangeUserName(string userName)
        {
            UserName = userName;
        }

        public void ChangeEmail(string newEmail)
        {
            Email = newEmail;
        }
    }
}