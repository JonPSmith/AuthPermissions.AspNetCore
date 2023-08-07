// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.SetupCode;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This service is designed for applications using the <see cref="TenantTypes.AddSharding"/>
/// Each tenant (shared or shard) needs a <see cref="ShardingEntry"/> entry to define the database
/// before you can create the tenant. And for delete of a shard tenant it will remove the
/// <see cref="ShardingEntry"/> entry if its <see cref="Tenant.HasOwnDb"/> is true.
/// </summary>
public interface IShardingOnlyTenantAddRemove
{
    /// <summary>
    /// This creates a tenant (shared or shard), and if that tenant is a shard (i.e. has its own database)
    /// it will create a sharding entry to contain the new database name
    /// (unless the <see cref="ShardingOnlyTenantAddDto.ShardingEntityName"/> isn't empty, when it will lookup the
    /// <see cref="ShardingEntry"/> defined by the <see cref="ShardingOnlyTenantAddDto.ShardingEntityName"/>).
    /// If a tenant that shares a database (tenant's HasOwnDb properly is false), then it use the <see cref="ShardingEntry"/> defined by the
    /// <see cref="ShardingOnlyTenantAddDto.ShardingEntityName"/> in the <see cref="ShardingOnlyTenantAddDto"/>.
    /// </summary>
    /// <param name="dto">A class called <see cref="ShardingOnlyTenantAddDto"/> holds all the data needed,
    /// including a method to validate that the information is correct.</param>
    /// <returns>status</returns>
    Task<IStatusGeneric> CreateTenantAsync(ShardingOnlyTenantAddDto dto);

    /// <summary>
    /// This will delete a tenant (shared or shard), and if that tenant <see cref="Tenant.HasOwnDb"/> is true
    /// it will also delete the <see cref="ShardingEntry"/> entry for this shard tenant.
    /// </summary>
    /// <param name="tenantId">The id of the tenant.</param>
    /// <returns>status</returns>
    Task<IStatusGeneric> DeleteTenantAsync(int tenantId);
}