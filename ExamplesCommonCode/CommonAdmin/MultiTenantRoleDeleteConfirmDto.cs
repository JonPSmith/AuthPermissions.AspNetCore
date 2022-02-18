// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Microsoft.EntityFrameworkCore;

namespace ExamplesCommonCode.CommonAdmin
{
    /// <summary>
    /// This is the multi-tenant delete confirm where you need to display what uses and tenants are using a Role
    /// </summary>
    public class MultiTenantRoleDeleteConfirmDto
    {
        public string RoleName { get; set; }
        public string ConfirmDelete { get; set; }
        public List<UserOrTenantDto> UsedBy { get; set; }

        public static async Task<MultiTenantRoleDeleteConfirmDto> FormRoleDeleteConfirmDtoAsync(string roleName, IAuthRolesAdminService rolesAdminService)
        {
            var result = new MultiTenantRoleDeleteConfirmDto
            {
                RoleName = roleName
            };
            result.UsedBy = (await rolesAdminService.QueryUsersUsingThisRole(roleName)
                    .Select(x => new { x.Email, x.UserName })
                    .ToListAsync())
                .Select(x => new UserOrTenantDto(true, x.UserName ?? x.Email))
                .ToList();
            result.UsedBy.AddRange(await rolesAdminService.QueryTenantsUsingThisRole(roleName)
                .Select(x => new UserOrTenantDto(false, x.TenantFullName))
                .ToListAsync());

            return result;
        }

    }
}