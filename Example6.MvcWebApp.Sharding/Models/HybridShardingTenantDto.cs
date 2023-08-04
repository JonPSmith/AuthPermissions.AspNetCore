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
    /// <summary>
    /// This class is designed for a hybrid multi-tenant app, e.g. a tenant can share its data a database with other tenants
    /// or a tenant can have their own database (sharding). The <see cref="HasOwnDb"/> determines which the tenant
    /// will use: false for share, true for sharding
    /// </summary>
    public class HybridShardingTenantDto
    {
        public int TenantId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantFullNameSize)]

        public string TenantName { get; set; }

        public string DataKey { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string ShardingName { get; set; }

        public bool HasOwnDb { get; set; }
        public List<string> AllShardingEntries { get; set; }

        public static IQueryable<HybridShardingTenantDto> TurnIntoDisplayFormat(IQueryable<Tenant> inQuery)
        {
            return inQuery.Select(x => new HybridShardingTenantDto
            {
                TenantId = x.TenantId,
                TenantName = x.TenantFullName,
                DataKey = x.GetTenantDataKey(),
                ShardingName = x.DatabaseInfoName,
                HasOwnDb = x.HasOwnDb
            });
        }

        public static HybridShardingTenantDto SetupForCreate(AuthPermissionsOptions options,
            List<string> allPossibleConnectionNames)
        {
            return new HybridShardingTenantDto
            {
                ShardingName = options.DefaultShardingEntryName,
                AllShardingEntries = allPossibleConnectionNames,
            };
        }

        public static async Task<HybridShardingTenantDto> SetupForUpdateAsync(IAuthTenantAdminService authTenantAdmin, int tenantId)
        {
            var tenant = (await authTenantAdmin.GetTenantViaIdAsync(tenantId)).Result;
            if (tenant == null)
                throw new AuthPermissionsException($"Could not find the tenant with a TenantId of {tenantId}");

            return new HybridShardingTenantDto
            {
                TenantId = tenantId,
                TenantName = tenant.TenantFullName,
                DataKey = tenant.GetTenantDataKey()
            };
        }
    }
}