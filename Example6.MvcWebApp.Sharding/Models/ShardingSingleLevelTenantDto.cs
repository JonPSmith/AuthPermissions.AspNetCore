// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;

namespace Example6.MvcWebApp.Sharding.Models
{
    public class ShardingSingleLevelTenantDto
    {
        public int TenantId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantFullNameSize)]
        public string TenantName { get; set; }

        public string DataKey { get; set; }


        [Required(AllowEmptyStrings = false)]
        public string ConnectionName { get; set; }

        public bool HasOwnDb { get; set; }
        public List<string> AllPossibleConnectionNames { get; set; }

        public static IQueryable<ShardingSingleLevelTenantDto> TurnIntoDisplayFormat(IQueryable<Tenant> inQuery)
        {
            return inQuery.Select(x => new ShardingSingleLevelTenantDto
            {
                TenantId = x.TenantId,
                TenantName = x.TenantFullName,
                DataKey = x.GetTenantDataKey(),
                ConnectionName = x.DatabaseInfoName,
                HasOwnDb = x.HasOwnDb
            });
        }

        public static ShardingSingleLevelTenantDto SetupForCreate(AuthPermissionsOptions options,
            List<string> allPossibleConnectionNames)
        {
            return new ShardingSingleLevelTenantDto
            {
                ConnectionName = options.ShardingDefaultDatabaseInfoName,
                AllPossibleConnectionNames = allPossibleConnectionNames,
            };
        }

        public static async Task<ShardingSingleLevelTenantDto> SetupForUpdateAsync(IAuthTenantAdminService authTenantAdmin, int tenantId)
        {
            var tenant = (await authTenantAdmin.GetTenantViaIdAsync(tenantId)).Result;
            if (tenant == null)
                throw new AuthPermissionsException($"Could not find the tenant with a TenantId of {tenantId}");

            return new ShardingSingleLevelTenantDto
            {
                TenantId = tenantId,
                TenantName = tenant.TenantFullName,
                DataKey = tenant.GetTenantDataKey()
            };
        }
    }
}