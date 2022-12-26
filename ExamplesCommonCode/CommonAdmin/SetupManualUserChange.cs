// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using StatusGeneric;

namespace ExamplesCommonCode.CommonAdmin
{
    public class SetupManualUserChange
    {
        /// <summary>
        /// This is used by SyncUsers to define what to do
        /// </summary>
        public SyncAuthUserChangeTypes FoundChangeType { get; set; }

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

        public static async Task<IStatusGeneric<SetupManualUserChange>> PrepareForUpdateAsync(string userId,
            IAuthUsersAdminService authUsersAdmin)
        {
            //This will work with localization, because the errors have already been localized.
            var status = new StatusGenericHandler<SetupManualUserChange>();
            var authUserStatus = await authUsersAdmin.FindAuthUserByUserIdAsync(userId);
            if (status.CombineStatuses(authUserStatus).HasErrors)
                return status;

            var authUser = authUserStatus.Result;

            var result = new SetupManualUserChange
            {
                FoundChangeType = SyncAuthUserChangeTypes.Update,
                UserId = authUser.UserId,
                UserName = authUser.UserName,
                Email = authUser.Email,
                RoleNames = authUser.UserRoles.Select(x => x.RoleName).ToList(),
                TenantName = authUser.UserTenant?.TenantFullName,
            };
            await result.SetupDropDownListsAsync(authUsersAdmin);

            return status.SetResult(result);
        }

        public static async Task<SetupManualUserChange> PrepareForCreateAsync(string userId, IAuthUsersAdminService authUsersAdmin)
        {
            var result = new SetupManualUserChange
            {
                FoundChangeType = SyncAuthUserChangeTypes.Create,
                UserId = userId,
            };
            await result.SetupDropDownListsAsync(authUsersAdmin);

            return result;
        }

        /// <summary>
        /// This will add/update an AuthUser using the information provided in this class
        /// </summary>
        /// <param name="authUsersAdmin"></param>
        /// <param name="localizeProvider"></param>
        /// <returns>Status</returns>
        public async Task<IStatusGeneric> ChangeAuthUserFromDataAsync(IAuthUsersAdminService authUsersAdmin,
            IAuthPDefaultLocalizer localizeProvider)
        {
            var status = new StatusGenericLocalizer(localizeProvider.DefaultLocalizer);

            switch (FoundChangeType)
            {
                case SyncAuthUserChangeTypes.NoChange:
                    status.SetMessageFormatted("SuccessNoChanges".ClassLocalizeKey(authUsersAdmin, true),
                    $"The user {UserName ?? Email} was marked as NoChange, so no change was applied");
                    break;
                case SyncAuthUserChangeTypes.Create:
                    status.CombineStatuses(
                        await authUsersAdmin.AddNewUserAsync(UserId, Email, UserName, RoleNames, TenantName));
                    break;
                case SyncAuthUserChangeTypes.Update:
                    status.CombineStatuses(
                        await authUsersAdmin.UpdateUserAsync(UserId, Email, UserName, RoleNames, TenantName));
                    break;
                case SyncAuthUserChangeTypes.Delete:
                    throw new AuthPermissionsException("You should direct a Delete change to a Delete confirm page.");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return status;
        }

        public async Task SetupDropDownListsAsync(IAuthUsersAdminService authUsersAdmin)
        {
            AllRoleNames = await authUsersAdmin.GetRoleNamesForUsersAsync(UserId);
            AllTenantNames = await authUsersAdmin.GetAllTenantNamesAsync();
            AllTenantNames.Insert(0, CommonConstants.EmptyItemName);
        }
    }
}