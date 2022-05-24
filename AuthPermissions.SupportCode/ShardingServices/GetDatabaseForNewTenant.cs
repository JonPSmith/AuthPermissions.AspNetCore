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

    /// <summary>
    /// This will look for a database for a new tenant.
    /// If the hasOwnDb is true, then it will find an empty database
    /// </summary>
    /// <param name="hasOwnDb"></param>
    /// <returns>Status with the DatabaseInfoName, or error if it can't find a database to work with</returns>
    public async Task<IStatusGeneric<string>> FindBestDatabaseInfoNameAsync(bool hasOwnDb)
    {
        var status = new StatusGenericHandler<string>();

        var dbsWithUsers = await _shardingService.GetDatabaseInfoNamesWithTenantNamesAsync();

        return status;
    }
}