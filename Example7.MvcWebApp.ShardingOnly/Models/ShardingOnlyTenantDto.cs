// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;

namespace Example7.MvcWebApp.ShardingOnly.Models
{
    /// <summary>
    /// This class is designed for a sharding-only type of tenant.
    /// </summary>
    public class ShardingOnlyTenantDto
    {
        public int TenantId { get; set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(AuthDbConstants.TenantFullNameSize)]

        public string TenantName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string ShardingName { get; set; }

        public List<string> AllShardingEntries { get; set; }

        public bool HasOwnDb => true;

        public static IQueryable<ShardingOnlyTenantDto> TurnIntoDisplayFormat(IQueryable<Tenant> inQuery)
        {
            return inQuery.Select(x => new ShardingOnlyTenantDto
            {
                TenantId = x.TenantId,
                TenantName = x.TenantFullName,
                ShardingName = x.DatabaseInfoName,
            });
        }

        public static ShardingOnlyTenantDto SetupForCreate(AuthPermissionsOptions options,
            List<string> allPossibleConnectionNames)
        {
            return new ShardingOnlyTenantDto
            {
                ShardingName = options.DefaultShardingEntryName,
                AllShardingEntries = allPossibleConnectionNames,
            };
        }

        public static async Task<ShardingOnlyTenantDto> SetupForUpdateAsync(IAuthTenantAdminService authTenantAdmin, int tenantId)
        {
            var tenant = (await authTenantAdmin.GetTenantViaIdAsync(tenantId)).Result;
            if (tenant == null)
                throw new AuthPermissionsException($"Could not find the tenant with a TenantId of {tenantId}");

            return new ShardingOnlyTenantDto
            {
                TenantId = tenantId,
                TenantName = tenant.TenantFullName
            };
        }
    }
}