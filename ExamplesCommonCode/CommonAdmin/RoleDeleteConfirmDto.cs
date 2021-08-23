// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Microsoft.EntityFrameworkCore;

namespace ExamplesCommonCode.CommonAdmin
{
    public class RoleDeleteConfirmDto
    {
        public string RoleName { get; set; }
        public string ConfirmDelete { get; set; }
        public List<EmailAndUserNameDto> AuthUsers { get; set; }

        public static async Task<RoleDeleteConfirmDto> FormRoleDeleteConfirmDtoAsync(string roleName, IAuthRolesAdminService rolesAdminService)
        {
            var result = new RoleDeleteConfirmDto();
            result.RoleName = roleName;
            result.AuthUsers = await rolesAdminService.QueryUsersUsingThisRole(roleName)
                .Select(x => new EmailAndUserNameDto(x.Email, x.UserName))
                .ToListAsync();

            return result;
        }

    }
}