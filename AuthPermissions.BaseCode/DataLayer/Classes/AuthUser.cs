// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using LocalizeMessagesAndErrors;
using StatusGeneric;

namespace AuthPermissions.BaseCode.DataLayer.Classes
{
    /// <summary>
    /// This defines a simple user (UserId, email and username) which will hold the roles and tenant data
    /// for this user.
    /// </summary>
    public class AuthUser : INameToShowOnException
    {
        private HashSet<UserToRole> _userRoles;

        private AuthUser() { } //Needed for EF Core

        private AuthUser(string userId, string email, string userName, List<RoleToPermissions> roles, Tenant userTenant)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));

            ChangeUserNameAndEmailWithChecks(email, userName);

            if (roles == null) throw new ArgumentNullException(nameof(roles));
            _userRoles = new HashSet<UserToRole>(roles.Select(x => new UserToRole(userId, x)));
            UserTenant = userTenant;
        }

        /// <summary>
        /// Define a user with there default roles and optional tenant
        /// </summary>
        /// <param name="userId">Id of the user - can't be null</param>
        /// <param name="email">user's email - especially useful in Web applications</param>
        /// <param name="userName">username - used when using Windows authentication. Generally useful for admin too.</param>
        /// <param name="roles">List of AuthP Roles for this user</param>
        /// <param name="localizeDefault">This provides the localize service</param>
        /// <param name="userTenant">optional: defines multi-tenant tenant for this user</param>
        public static IStatusGeneric<AuthUser> CreateAuthUser(string userId, string email,
            string userName, List<RoleToPermissions> roles, IDefaultLocalizer localizeDefault,
            Tenant userTenant = null)
        {
            var status = new StatusGenericLocalizer<AuthUser>(localizeDefault);

            status.CombineStatuses(CheckRolesAreValidForUser(roles, userTenant != null, localizeDefault));
            if (status.HasErrors)
                return status;

            return status.SetResult(new AuthUser(userId, email, userName, roles, userTenant));
        }

        /// <summary>
        /// The user's Id is its primary key
        /// </summary>
        [Key]
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)]
        public string UserId { get; private set; }

        /// <summary>
        /// Contains a unique Email, which is used for lookup
        /// (can be null if using Windows authentication provider)
        /// </summary>
        [MaxLength(AuthDbConstants.EmailSize)]
        public string Email { get; private set; }

        /// <summary>
        /// Contains a unique user name
        /// This is used to a) provide more info on the user, or b) when using Windows authentication provider
        /// </summary>
        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get; private set; }

        /// <summary>
        /// If true the user is disabled, which means no AuthP claims will be added to its claims
        /// NOTE: By default this does not stop this user from logging in
        /// </summary>
        public bool IsDisabled { get; private set; }

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
        public string NameToUseForError
        {
            get
            {
                if (Email != null && UserName != null && Email != UserName)
                    //If both the Email and the UserName are set, and aren't the same we show both
                    return $"{Email} or {UserName}";

                return UserName ?? Email;
            }
        }


        /// <summary>
        /// Summary of AuthUser
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var tenantString = TenantId == null ? "" 
                : (UserTenant == null ? ", has an tenant" : $", linked to {UserTenant.TenantFullName}");
            var rolesString = _userRoles == null ? "" : $", roles = {string.Join(", ", _userRoles.Select(x => x.RoleName))}";
            return $"UserName = {UserName}, Email = {Email}, UserId = {UserId}{rolesString}{tenantString}.";
        }

        //--------------------------------------------------
        // Access methods

        /// <summary>
        /// This will replace all the Roles for this AuthUser
        /// </summary>
        /// <param name="roles">List of roles to replace the current user's roles</param>
        /// <param name="localizeDefault"></param>
        public IStatusGeneric ReplaceAllRoles(List<RoleToPermissions> roles, IDefaultLocalizer localizeDefault)
        {
            if (_userRoles == null)
                throw new AuthPermissionsException($"You must load the {nameof(UserRoles)} before calling this method");

            var status = new StatusGenericLocalizer(localizeDefault);

            status.CombineStatuses(CheckRolesAreValidForUser(roles, TenantId != null, localizeDefault));
            if (status.HasErrors)
                return status;

            _userRoles = new HashSet<UserToRole>(roles.Select(x => new UserToRole(UserId, x)));

            return status;
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

        /// <summary>
        /// This changes the email and username, which checks that at least one of them isn't null
        /// </summary>
        /// <param name="email"></param>
        /// <param name="userName"></param>
        public void ChangeUserNameAndEmailWithChecks(string email, string userName)
        {
            Email = email?.Trim();
            UserName = (userName?.Trim() ?? Email) ?? throw new AuthPermissionsBadDataException(
                $"The {nameof(Email)} and {nameof(UserName)} can't both be null.");

            Email = Email?.ToLower(); //make email lower case as Postgres string compare is case sensitive
        }

        /// <summary>
        /// This allows you to change the user's <see cref="IsDisabled"/> setting
        /// </summary>
        /// <param name="isDisabled">If true, then no AuthP claims are adding the the user's claims</param>
        public void UpdateIsDisabled(bool isDisabled)
        {
            IsDisabled = isDisabled;
        }

        //---------------------------------------------------------
        // private methods

        /// <summary>
        /// This checks that the roles are valid for this type of user
        /// </summary>
        /// <param name="foundRoles"></param>
        /// <param name="tenantUser"></param>
        /// <param name="localizeDefault">This provides the localize service</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static IStatusGeneric CheckRolesAreValidForUser(List<RoleToPermissions> foundRoles, bool tenantUser,
            IDefaultLocalizer localizeDefault)
        {
            var status = new StatusGenericLocalizer(localizeDefault);

            foreach (var foundRole in foundRoles)
            {
                switch (tenantUser)
                {
                    case true when foundRole.RoleType == RoleTypes.HiddenFromTenant:
                        status.AddErrorFormatted("InvalidRoleHidden".StaticClassLocalizeKey(typeof(AuthUser), true),
                            $"You cannot add the role '{foundRole.RoleName}' to an Auth tenant user because it can only be used by the App Admin.");
                        break;
                    case true when foundRole.RoleType == RoleTypes.TenantAutoAdd:
                        status.AddErrorFormatted("InvalidRoleAutoAdd".StaticClassLocalizeKey(typeof(AuthUser), true),
                        $"You cannot add the role '{foundRole.RoleName}' to an Auth tenant user because it is automatically to tenant users.");
                        break;
                }
            }

            return status;
        }
    }
}