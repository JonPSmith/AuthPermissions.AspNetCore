// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace Example4.MvcWebApp.IndividualAccounts.Models
{
    public class AuthUserUpdate
    {
        /// <summary>
        /// This is used by SyncUsers to define what to do
        /// Not used in edit
        /// </summary>
        public SyncAuthUserChanges FoundChange { get; set; }

        /// <summary>
        /// The userId of the user (NOTE: this is not show 
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.UserIdSize)] 
        public string UserId { get; set; }
        /// <summary>
        /// The user's main email (used as one way to find the user) 
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.EmailSize)] 
        public string Email { get; set; }
        /// <summary>
        /// The user's name
        /// </summary>
        [MaxLength(AuthDbConstants.UserNameSize)]
        public string UserName { get; set; }

        /// <summary>
        /// The AuthRoles for this AuthUser
        /// </summary>
        public List<string> RoleNames { set; get; } 

        /// <summary>
        /// The name of the AuthP Tenant for this AuthUser (can be null)
        /// </summary>
        public string TenantName { set; get; }

        public List<string> AllRoleNames { get; set; }

        public static async Task<IStatusGeneric<AuthUserUpdate>> BuildAuthUserUpdateAsync(string userId, IAuthUsersAdminService authUsersAdmin, AuthPermissionsDbContext context)
        {
            var status = new StatusGenericHandler<AuthUserUpdate>();
            var authUserStatus = await GetAuthUserAsync(userId, authUsersAdmin);
            if (status.CombineStatuses(authUserStatus).HasErrors)
                return status;

            var authUser = authUserStatus.Result;

            var result = new AuthUserUpdate
            {
                UserId = authUser.UserId,
                UserName = authUser.UserName,
                Email = authUser.Email,
                RoleNames = authUser.UserRoles.Select(x => x.RoleName).ToList(),
                TenantName = authUser.UserTenant?.TenantName,

                AllRoleNames = await context.RoleToPermissions.Select(x => x.RoleName).ToListAsync()
            };
            await result.SetupAllRoleNamesAsync(context);

            return status.SetResult(result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authUsersAdmin"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<IStatusGeneric> UpdateAuthUserFromDataAsync(IAuthUsersAdminService authUsersAdmin, AuthPermissionsDbContext context)
        {
            var status = new StatusGenericHandler {Message = $"Successfully updated the user {UserName ?? Email}"};
            var authUserStatus = await GetAuthUserAsync(UserId, authUsersAdmin);
            if (status.CombineStatuses(authUserStatus).HasErrors)
                return status;

            var authUser = authUserStatus.Result;

            authUser.ChangeUserNameAndEmail(UserName, Email);
            if (authUser.UserRoles != null && authUser.UserRoles.Select(x => x.RoleName).OrderBy(x => x) !=
                RoleNames.OrderBy(x => x))
            {
                //The Roles have changed, so we have to change the roles
                var foundRoles = await context.RoleToPermissions
                    .Where(x => RoleNames.Contains(x.RoleName))
                    .ToListAsync();
                if (foundRoles.Count != RoleNames.Count)
                    throw new AuthPermissionsBadDataException("One or more role names weren't found in the database.");

                authUser.ReplaceAllRoles(foundRoles);
            }

            if (authUser.UserTenant?.TenantName != TenantName)
            {
                //Change of tenant
                if (string.IsNullOrEmpty( TenantName))
                    //remove the tenant 
                    authUser.UpdateUserTenant(null);
                else
                {
                    var foundTenant = await context.Tenants.SingleOrDefaultAsync(x => x.TenantName == TenantName);
                    if (foundTenant == null)
                        return status.AddError($"A tenant with the name {TenantName} wasn't found.");
                }
            }

            status.CombineStatuses(await context.SaveChangesWithChecksAsync());
            return status;
        }

        public async Task SetupAllRoleNamesAsync(AuthPermissionsDbContext context)
        {
            AllRoleNames = await context.RoleToPermissions.Select(x => x.RoleName).ToListAsync();
        }

        private static async Task<IStatusGeneric<AuthUser>> GetAuthUserAsync(string userId, IAuthUsersAdminService authUsersAdmin)
        {
            var status = new StatusGenericHandler<AuthUser>();
            var authUser = await authUsersAdmin.FindAuthUserByUserIdAsync(userId);
            if (authUser == null)
                status.AddError("Could not find the AuthP User you asked for.");

            return status.SetResult(authUser);
        }
    }
}