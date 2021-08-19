// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace ExamplesCommonCode.CommonAdmin
{
    public class SetupManualUserChange
    {
        /// <summary>
        /// This is used by SyncUsers to define what to do
        /// </summary>
        public SyncAuthUserChanges FoundChange { get; set; }

        /// <summary>
        /// The userId of the user (NOTE: this is not show)
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

        public static async Task<IStatusGeneric<SetupManualUserChange>> PrepareForUpdateAsync(string userId, IAuthUsersAdminService authUsersAdmin, AuthPermissionsDbContext context)
        {
            var status = new StatusGenericHandler<SetupManualUserChange>();
            var authUserStatus = await authUsersAdmin.FindAuthUserByUserIdAsync(userId);
            if (status.CombineStatuses(authUserStatus).HasErrors)
                return status;

            var authUser = authUserStatus.Result;

            var result = new SetupManualUserChange
            {
                FoundChange = SyncAuthUserChanges.Update,
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

        public static async Task<SetupManualUserChange> PrepareForCreateAsync(string userId, AuthPermissionsDbContext context)
        {
            var result = new SetupManualUserChange
            {
                FoundChange = SyncAuthUserChanges.Create,
                UserId = userId,
            };
            await result.SetupDropDownListsAsync(context);

            return result;
        }

        /// <summary>
        /// This will add/update an AuthUser using the information provided in this class
        /// </summary>
        /// <param name="authUsersAdmin"></param>
        /// <param name="context"></param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> ChangeAuthUserFromDataAsync(IAuthUsersAdminService authUsersAdmin, AuthPermissionsDbContext context)
        {
            var status = new StatusGenericHandler();

            switch (FoundChange)
            {
                case SyncAuthUserChanges.NoChange:
                    status.Message = $"The user {UserName ?? Email} was marked as NoChange, so no change was applied";
                    break;
                case SyncAuthUserChanges.Create:
                    status.CombineStatuses(
                        await authUsersAdmin.AddNewUserAsync(UserId, Email, UserName, RoleNames, TenantName));
                    break;
                case SyncAuthUserChanges.Update:
                    status.CombineStatuses(
                        await authUsersAdmin.UpdateUserAsync(UserId, Email, UserName, RoleNames, TenantName));
                    break;
                case SyncAuthUserChanges.Delete:
                    throw new AuthPermissionsException("You should direct a Delete change to a Delete confirm page.");
                default:
                    throw new ArgumentOutOfRangeException();
            }

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