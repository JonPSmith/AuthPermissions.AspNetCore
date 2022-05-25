// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BaseCode;
using StatusGeneric;

namespace AuthPermissions.SupportCode.ShardingServices;

/// <summary>
/// This provides a way to find a database when using sharding.
/// It will use the <see cref="AuthPermissionsOptions.ShardingPercentHasOwnDbs"/> option,
/// which has the ratio of sharding vs. shared databases, to pick a database
/// </summary>
public class GetDatabaseForNewTenant : IGetDatabaseForNewTenant
{
    private readonly IShardingConnections _shardingService;

    public GetDatabaseForNewTenant(IShardingConnections shardingService)
    {
        _shardingService = shardingService;
    }

    /// <summary>
    /// This will look for a database for a new tenant.
    /// If the hasOwnDb is true, then it will find an empty database,
    /// otherwise it will look for database containing multiple tenants
    /// </summary>
    /// <param name="hasOwnDb">If true the tenant needs its own database. False means it shares a database.</param>
    /// <param name="region">If not null this provides geographic information to pick the nearest database server.</param>
    /// <returns>Status with the DatabaseInfoName, or error if it can't find a database to work with</returns>
    public async Task<IStatusGeneric<string>> FindBestDatabaseInfoNameAsync(bool hasOwnDb, string region)
    {
        var status = new StatusGenericHandler<string>();

        var dbsWithUsers = await _shardingService.GetDatabaseInfoNamesWithTenantNamesAsync();

        return status;
    }
}