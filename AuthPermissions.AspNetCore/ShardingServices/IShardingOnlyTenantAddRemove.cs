// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.SetupCode;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This service is designed for applications using the <see cref="TenantTypes.AddSharding"/> feature
/// and this code creates / deletes a shard tenant (i.e. the tenant's <see cref="Tenant.HasOwnDb"/> is true) and
/// at the same time adds / removes a <see cref="ShardingEntry"/> entry linked to the tenant's database.
/// </summary>
public interface IShardingOnlyTenantAddRemove
{
    /// <summary>
    /// This creates a shard tenant (i.e. the tenant's <see cref="Tenant.HasOwnDb"/> is true) and 
    /// it will create a sharding entry to contain the new database name.
    /// Note this method can handle single and hierarchical tenants, including adding a child
    /// hierarchical entry which uses the parent's sharding entry. 
    /// </summary>
    /// <param name="dto">A class called <see cref="ShardingOnlyTenantAddDto"/> holds all the data needed,
    /// including a method to validate that the information is correct.</param>
    /// <returns>status</returns>
    Task<IStatusGeneric> CreateTenantAsync(ShardingOnlyTenantAddDto dto);

    /// <summary>
    /// This will delete a shard tenant (i.e. the tenant's <see cref="Tenant.HasOwnDb"/> is true)
    /// and will also delete the <see cref="ShardingEntry"/> entry for this shard tenant
    /// (unless the tenant is a child hierarchical, in which case it doesn't delete the <see cref="ShardingEntry"/> entry).
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    /// <returns>status</returns>
    Task<IStatusGeneric> DeleteTenantAsync(int tenantId);
}