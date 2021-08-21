// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using Microsoft.AspNetCore.Mvc.Rendering;
using StatusGeneric;

namespace Example1.RazorPages.IndividualAccounts.Model
{
    public class EditDto
    {
        /// <summary>
        /// The userId of the user (NOTE: this is not shown)
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
        public SelectList RolesSelectList { set; get; }

        public List<string> SelectedRoleNames { get; set; }

        public static async Task<IStatusGeneric<EditDto>> PrepareForUpdateAsync(string userId,
            IAuthUsersAdminService authUsersAdmin)
        {
            var status = new StatusGenericHandler<EditDto>();
            var authUserStatus = await authUsersAdmin.FindAuthUserByUserIdAsync(userId);
            if (status.CombineStatuses(authUserStatus).HasErrors)
                return status;

            var authUser = authUserStatus.Result;
            var allRoleNames = await authUsersAdmin.GetAllRoleNamesAsync();

            var result = new EditDto
            {
                UserId = authUser.UserId,
                UserName = authUser.UserName,
                Email = authUser.Email,
                SelectedRoleNames = authUser.UserRoles.Select(x => x.RoleName).ToList(),
                RolesSelectList = new SelectList(allRoleNames, authUser.UserRoles.Select(x => x.RoleName))
            };

            return status.SetResult(result);
        }
    }
}