// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode.Services.Internal;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
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
        private readonly IDefaultLocalizer _localizeDefault;
        private readonly Type _permissionType;
        private readonly bool _isMultiTenant;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="context"></param>
        /// <param name="options"></param>
        /// <param name="localizeProvider"></param>
        public AuthRolesAdminService(AuthPermissionsDbContext context, AuthPermissionsOptions options,
            IAuthPDefaultLocalizer localizeProvider)
        {
            _context = context;
            _localizeDefault = localizeProvider.DefaultLocalizer;
            _permissionType = options.InternalData.EnumPermissionsType;
            _isMultiTenant = options.TenantType.IsMultiTenant();
        }

        /// <summary>
        /// This simply returns a IQueryable of the <see cref="RoleWithPermissionNamesDto"/>.
        /// This contains all the properties in the <see cref="RoleToPermissions"/> class, plus a list of the Permissions names
        /// This can be by a user linked to a tenant and it will display all the roles that tenant can use 
        /// </summary>
        /// <param name="currentUserId">Only used if using AuthP's multi-tenant feature you must provide the current user's ID</param>
        /// <returns>query on the database</returns>
        public IQueryable<RoleWithPermissionNamesDto> QueryRoleToPermissions(string currentUserId = null)
        {
            if (!_isMultiTenant)
                return MapToRoleWithPermissionNamesDto(_context.RoleToPermissions);

            //multi-tenant version has to filter out the roles from users that have a tenant
            var tenantId = FindTheTenantIdOfTheUser(currentUserId);

            return tenantId == null
                ? MapToRoleWithPermissionNamesDto(_context.RoleToPermissions)
                : MapToRoleWithPermissionNamesDto(_context.RoleToPermissions
                    .Where(x => x.RoleType == RoleTypes.Normal
                                || (x.RoleType == RoleTypes.TenantAutoAdd || x.RoleType == RoleTypes.TenantAdminAdd)
                                   & x.Tenants.Select(y => y.TenantId).Contains((int)tenantId)));
        }

        /// <summary>
        /// This returns a list of permissions with the information from the Display attribute
        /// NOTE: This should not be called by a user that has a tenant, but this isn't checked
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
        /// NOTE: it assumes that the user can only look for roles that they are allowed to see
        /// </summary>
        public IQueryable<AuthUser> QueryUsersUsingThisRole(string roleName)
        {
            return _context.AuthUsers.Where(x => x.UserRoles.Any(y => y.RoleName == roleName));
        }

        /// <summary>
        /// This returns a query containing all the Tenants that have given role name
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public IQueryable<Tenant> QueryTenantsUsingThisRole(string roleName)
        {
            return _context.Tenants.Where(x => x.TenantRoles.Any(y => y.RoleName == roleName));
        }


        /// <summary>
        /// This adds a new RoleToPermissions with the given description and permissions defined by the names 
        /// </summary>
        /// <param name="roleName">Name of the new role (must be unique)</param>
        /// <param name="permissionNames">a collection of permission names to go into this role</param>
        /// <param name="description">The description to tell you what this role allows the user to use - can be null</param>
        /// <param name="roleType">Optional: defaults to <see cref="RoleTypes.Normal"/></param>
        /// <returns>A status with any errors found</returns>
        public async Task<IStatusGeneric> CreateRoleToPermissionsAsync(string roleName,
            IEnumerable<string> permissionNames,
            string description, RoleTypes roleType = RoleTypes.Normal)
        {
            var status = new StatusGenericLocalizer(_localizeDefault);
            status.SetMessageFormatted("Success".ClassMethodLocalizeKey(this, true), 
                $"Successfully added the new role {roleName}.");

            if (string.IsNullOrEmpty(roleName))
                return status.AddErrorString("BadRoleName".ClassMethodLocalizeKey(this, true), 
                    "The RoleName isn't filled in", nameof(roleName).CamelToPascal());
            if ((await _context.RoleToPermissions.SingleOrDefaultAsync(x => x.RoleName == roleName)) != null)
                return status.AddErrorFormattedWithParams("DuplicateRoleName".ClassMethodLocalizeKey(this, true),
                    $"There is already a Role with the name of '{roleName}'.", nameof(roleName).CamelToPascal());
            
            if (permissionNames == null)
                return status.AddErrorString("NoPermissions".ClassLocalizeKey(this, true), //common error
                    "You must provide at least one permission name.", 
                    permissionNames.Select(y => y.CamelToPascal()).ToArray());

            //NOTE: If an advanced permission (i.e. has the display attribute has AutoGenerateFilter = true) is found the roleType is updated to HiddenFromTenant
            var packedPermissions = _permissionType.PackPermissionsNamesWithValidation(permissionNames,
                x => status.AddErrorFormattedWithParams("InvalidPermission".ClassLocalizeKey(this, true), //common error
                    $"The permission name '{x}' isn't a valid name in the {_permissionType.Name} enum.",
                    permissionNames.Select(y => y.CamelToPascal()).ToArray()), () => roleType = RoleTypes.HiddenFromTenant);

            if (status.HasErrors)
                return status;

            _context.Add(new RoleToPermissions(roleName, description, packedPermissions, roleType));
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));

            return status;
        }

        /// <summary>
        /// This updates the role's permission names, and optionally its description
        /// if the new permissions contain an advanced permission
        /// </summary>
        /// <param name="roleName">Name of an existing role</param>
        /// <param name="permissionNames">a collection of permission names to go into this role</param>
        /// <param name="description">Optional: If given then updates the description for this role</param>
        /// <param name="roleType">Optional: defaults to <see cref="RoleTypes.Normal"/>.
        /// NOTE: the roleType is changed to <see cref="RoleTypes.HiddenFromTenant"/> if advanced permissions are found</param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> UpdateRoleToPermissionsAsync(string roleName,
            IEnumerable<string> permissionNames,
            string description, RoleTypes roleType = RoleTypes.Normal)
        {
            var status = new StatusGenericLocalizer(_localizeDefault);
            status.SetMessageFormatted("Success".ClassMethodLocalizeKey(this, true),
                $"Successfully updated the role {roleName}.");
            var existingRolePermission = await _context.RoleToPermissions.SingleOrDefaultAsync(x => x.RoleName == roleName);

            if (existingRolePermission == null)
                return status.AddErrorFormattedWithParams("IncorrectRoleName".ClassLocalizeKey(this, true), //common error in this class
                    $"Could not find a role called {roleName}", nameof(roleName).CamelToPascal());

            var originalRoleType = existingRolePermission.RoleType;

            var packedPermissions = _permissionType.PackPermissionsNamesWithValidation(permissionNames,
                x => status.AddErrorFormattedWithParams("InvalidPermission".ClassLocalizeKey(this, true), //common error
                    $"The permission name '{x}' isn't a valid name in the {_permissionType.Name} enum.", 
                    permissionNames.Select(y => y.CamelToPascal()).ToArray()), 
                () => roleType = RoleTypes.HiddenFromTenant);

            if (status.HasErrors)
                return status;

            if (!packedPermissions.Any())
                return status.AddErrorString("NoPermissions".ClassLocalizeKey(this, true), //common error 
                    "You must provide at least one permission name.", 
                    permissionNames.Select(y => y.CamelToPascal()).ToArray());

            if (originalRoleType != roleType)
            {
                //We need to check that the new RoleType matches where they are used
                var roleChecker = new ChangeRoleTypeChecks(_context);
                if (status.CombineStatuses(
                        await roleChecker.CheckRoleTypeChangeAsync(originalRoleType, roleType,roleName, _localizeDefault)).HasErrors)
                    return status;
            }

            existingRolePermission.Update(packedPermissions, description, roleType);
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));

            return status;
        }

        /// <summary>
        /// This deletes a Role. If that Role is already assigned to AuthP users you must set the removeFromUsers to true
        /// otherwise you will get an error.
        /// </summary>
        /// <param name="roleName">name of role to delete</param>
        /// <param name="removeFromUsers">If false it will fail if any AuthP user have that role.
        ///     If true it will delete the role from all the users that have it.</param>
        /// <returns>status</returns>
        public async Task<IStatusGeneric> DeleteRoleAsync(string roleName, bool removeFromUsers)
        {
            var status = new StatusGenericLocalizer(_localizeDefault);

            var existingRolePermission =
                await _context.RoleToPermissions.SingleOrDefaultAsync(x => x.RoleName == roleName);

            if (existingRolePermission == null)
                return status.AddErrorFormattedWithParams("IncorrectRoleName".ClassLocalizeKey(this, true), //common error in this class
                    $"Could not find a role called {roleName}", nameof(roleName).CamelToPascal());

            var usersWithRoles = await _context.UserToRoles.Where(x => x.RoleName == roleName).ToListAsync();
            int tenantCount = existingRolePermission.RoleType == RoleTypes.TenantAdminAdd || existingRolePermission.RoleType == RoleTypes.TenantAutoAdd
                ? await QueryTenantsUsingThisRole(roleName).CountAsync() : 0;
            if (!removeFromUsers)
            {
                if (usersWithRoles.Any())
                    status.AddErrorFormattedWithParams("RoleUsedUser".ClassMethodLocalizeKey(this, true),
                        $"That role is used in {usersWithRoles.Count} AuthUsers and you didn't confirm the delete.", 
                    nameof(roleName).CamelToPascal());

                if (tenantCount > 0)
                    status.AddErrorFormattedWithParams("RoleUsedTenant".ClassMethodLocalizeKey(this, true),
                        $"That role is used in {usersWithRoles.Count} tenants and you didn't confirm the delete.",
                        nameof(roleName).CamelToPascal());

                if (status.HasErrors)
                    return status;
            }

            if (usersWithRoles.Any())
            {
                _context.RemoveRange(usersWithRoles);
            }

            if (status.HasErrors)
                return status;

            _context.Remove(existingRolePermission);
            status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));

            //build the success message
            var successMessages = new List<FormattableString> { $"Successfully deleted the role {roleName}" };
            var successKey = "Success";
            if (usersWithRoles.Any())
            {
                successMessages.Add( $" and removed that role from {usersWithRoles.Count} users");
                successKey += "-RemoveUsers";
            }
            if (tenantCount > 0)
            {
                successMessages.Add($" and removed that role from {tenantCount} tenants");
                successKey += "-RemoveTenants";
            }
            successMessages.Add($".");
            //There are 4 possible keys for this method
            //1. "Success":                           delete Role, doesn't effect anyone
            //2. "Success-RemoveUsers":               delete Role, affects users
            //3. "Success-RemoveTenants":             delete Role, affects tenants
            //4. "Success-RemoveUsers-RemoveTenants": delete Role, affects users and tenants
            status.SetMessageFormatted(successKey.ClassMethodLocalizeKey(this, true), successMessages.ToArray());
            return status;
        }

        //---------------------------------------------------------
        // private methods

        private IQueryable<RoleWithPermissionNamesDto> MapToRoleWithPermissionNamesDto(
            IQueryable<RoleToPermissions> roleToPermissions)
        {
            return roleToPermissions.Select(x => new RoleWithPermissionNamesDto
            {
                RoleName = x.RoleName,
                Description = x.Description,
                RoleType = x.RoleType,
                PackedPermissionsInRole = x.PackedPermissionsInRole,
                PermissionNames = x.PackedPermissionsInRole.ConvertPackedPermissionToNames(_permissionType)
            });
        }

        /// <summary>
        /// Used to find the tenantId of the current user - can be null if not an tenant user
        /// </summary>
        /// <param name="currentUserId"></param>
        /// <returns></returns>
        private int? FindTheTenantIdOfTheUser(string currentUserId)
        {
            if (currentUserId == null)
                throw new ArgumentNullException(nameof(currentUserId), "You must be logged in to use this feature.");

            var tenantId = _context.AuthUsers
                .Where(x => x.UserId == currentUserId)
                .Select(x => x.TenantId)
                .SingleOrDefault();
            return tenantId;
        }
    }
}