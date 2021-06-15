// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.DataLayer.Classes
{
    /// <summary>
    /// This defines a simple user (UserId, email and username) which will hold the roles and tenant data
    /// for this user.
    /// </summary>
    [Index(nameof(Email), IsUnique = true)]
    public class AuthUser
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
        /// The user's Id is its primamry key
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
        // Access methods

        /// <summary>
        /// This finds the  a UserToRole after checks that it is allowable
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> AddRoleToUserWithChecksAsync(string userId, string roleName,
            AuthPermissionsDbContext context)
        {
            if (roleName == null) throw new ArgumentNullException(nameof(roleName));

            var status = new StatusGenericHandler<UserToRole>();
            var roleToAdd = await context.RoleToPermissions.SingleOrDefaultAsync(x => x.RoleName == roleName);
            if (roleToAdd == null)
                return status.AddError($"I could not find the Role '{roleName}'.");

            _userRoles.Add(new UserToRole(userId, roleToAdd));

            return status;
        }
    }
}