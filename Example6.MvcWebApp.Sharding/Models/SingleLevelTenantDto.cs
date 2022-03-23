// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AuthPermissions.AdminCode;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;

namespace Example6.MvcWebApp.Sharding.Models
{
    public class SingleLevelTenantDto
    {
        public int TenantId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantFullNameSize)]
        public string TenantName { get; set; }

        public string DataKey { get; set; }

        public List<string> TenantRolesName { get; set; }

        public List<string> AllPossibleRoleNames { get; set; }


        public static IQueryable<SingleLevelTenantDto> TurnIntoDisplayFormat(IQueryable<Tenant> inQuery)
        {
            return inQuery.Select(x => new SingleLevelTenantDto
            {
                TenantId = x.TenantId,
                TenantName = x.TenantFullName,
                DataKey = x.GetTenantDataKey(),
                TenantRolesName = x.TenantRoles.Select(x => x.RoleName).ToList()
            });
        }

        public static async Task<SingleLevelTenantDto> SetupForUpdateAsync(IAuthTenantAdminService authTenantAdmin, int tenantId)
        {
            var tenant = (await authTenantAdmin.GetTenantViaIdAsync(tenantId)).Result;
            if (tenant == null)
                throw new AuthPermissionsException($"Could not find the tenant with a TenantId of {tenantId}");

            return new SingleLevelTenantDto
            {
                TenantId = tenantId,
                TenantName = tenant.TenantFullName,
                DataKey = tenant.GetTenantDataKey(),
                TenantRolesName = tenant.TenantRoles.Select(x => x.RoleName).ToList(),
                AllPossibleRoleNames = await authTenantAdmin.GetRoleNamesForTenantsAsync()
            };
        }
    }
}