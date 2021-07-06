// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace ExamplesCommonCode.CommonAdmin
{
    public class AuthUserChange
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

        public List<string> AllTenantNames { get; set; }

        public static async Task<IStatusGeneric<AuthUserChange>> BuildAuthUserUpdateAsync(string userId, IAuthUsersAdminService authUsersAdmin, AuthPermissionsDbContext context)
        {
            var status = new StatusGenericHandler<AuthUserChange>();
            var authUserStatus = await authUsersAdmin.FindAuthUserByUserIdAsync(userId);
            if (status.CombineStatuses(authUserStatus).HasErrors)
                return status;

            var authUser = authUserStatus.Result;

            var result = new AuthUserChange
            {
                UserId = authUser.UserId,
                UserName = authUser.UserName,
                Email = authUser.Email,
                RoleNames = authUser.UserRoles.Select(x => x.RoleName).ToList(),
                TenantName = authUser.UserTenant?.TenantFullName,

                AllRoleNames = await context.RoleToPermissions.Select(x => x.RoleName).ToListAsync()
            };
            await result.SetupDropDownListsAsync(context);

            return status.SetResult(result);
        }

        /// <summary>
        /// This will add/update an AuthUser using the information provided in this class
        /// </summary>
        /// <param name="authUsersAdmin"></param>
        /// <param name="context"></param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> ChangeAuthUserFromDataAsync(IAuthUsersAdminService authUsersAdmin, AuthPermissionsDbContext context)
        {
            var status = new StatusGenericHandler {Message = $"Successfully updated the user {UserName ?? Email}"};

            //find the roles
            var foundRoles = RoleNames?.Any() == true
                ? await context.RoleToPermissions
                    .Where(x => RoleNames.Contains(x.RoleName))
                    .ToListAsync()
                : new List<RoleToPermissions>();
            if (foundRoles.Count != (RoleNames?.Count ?? 0))
                throw new AuthPermissionsBadDataException("One or more role names weren't found in the database.");

            //Find the tenant
            var foundTenant = string.IsNullOrEmpty(TenantName) || TenantName == CommonConstants.EmptyTenantName
                ? null
                : await context.Tenants.SingleOrDefaultAsync(x => x.TenantFullName == TenantName);
            if (!string.IsNullOrEmpty(TenantName) && TenantName != CommonConstants.EmptyTenantName && foundTenant == null)
                return status.AddError($"A tenant with the name {TenantName} wasn't found.");

            if (FoundChange == SyncAuthUserChanges.Add)
            {
                status.Message = $"Successfully added a AuthUser with the name {UserName ?? Email}";

                var authUser = new AuthUser(UserId, Email, UserName, foundRoles, foundTenant);
                context.Add(authUser);
            }
            else
            {
                status.Message = $"Successfully updated the AuthUser with the name {UserName ?? Email}";

                var authUserStatus = await authUsersAdmin.FindAuthUserByUserIdAsync(UserId);
                if (status.CombineStatuses(authUserStatus).HasErrors)
                    return status;

                var authUser = authUserStatus.Result;

                //Its an update
                authUser.ChangeUserNameAndEmail(UserName, Email);
                if (foundRoles.Any() && authUser.UserRoles.Select(x => x.RoleName).OrderBy(x => x) !=
                        RoleNames.OrderBy(x => x))
                    //The roles are different so 
                    authUser.ReplaceAllRoles(foundRoles);
                if (authUser.UserTenant?.TenantFullName != TenantName)
                {
                    authUser.UpdateUserTenant(string.IsNullOrEmpty(TenantName) ? null : foundTenant);
                }
            }

            status.CombineStatuses(await context.SaveChangesWithChecksAsync());
            return status;
        }

        public async Task SetupDropDownListsAsync(AuthPermissionsDbContext context)
        {
            AllRoleNames = await context.RoleToPermissions.Select(x => x.RoleName).ToListAsync();
            AllTenantNames = await context.Tenants.Select(x => x.TenantFullName).ToListAsync();
            AllTenantNames.Insert(0, CommonConstants.EmptyTenantName);
        }
    }
}