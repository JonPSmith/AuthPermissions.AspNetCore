// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using StatusGeneric;

namespace AuthPermissions.SupportCode;

/// <summary>
/// This is a demo implementation of the <see cref="ISignUpGetShardingEntry"/> interface
/// used with a Hybrid sharding where all the <see cref="ShardingEntry"/> have already been
/// created bt the App Admin.
/// </summary>
public class DemoGetDatabaseForNewTenant : ISignUpGetShardingEntry
{
    private readonly IDefaultLocalizer _localizeDefault;
    private readonly IGetSetShardingEntries _shardingService;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="shardingService"></param>
    /// <param name="localizeProvider"></param>
    public DemoGetDatabaseForNewTenant(IGetSetShardingEntries shardingService,
        IAuthPDefaultLocalizer localizeProvider)
    {
        _shardingService = shardingService;
        _localizeDefault = localizeProvider.DefaultLocalizer;
    }

    /// <summary>
    /// This will allow you to find of create a <see cref="ShardingEntry"/> for the new sharding tenant
    /// and return the existing / new <see cref="ShardingEntry"/>'s Name.
    /// 1. Hybrid sharding: you might have existing <see cref="ShardingEntry"/> / databases or might create a
    /// new <see cref="ShardingEntry"/>.
    /// 2. Sharding-only: In this case you will be creating new <see cref="ShardingEntry"/>
    /// </summary>
    /// <param name="hasOwnDb">If true the tenant needs its own database. False means it shares a database.</param>
    /// <param name="createTimestamp">If you create a new <see cref="ShardingEntry"/> you should include this timestamp
    /// in the name of the entry. This is useful to the App Admin when looking at a SignUp that failed.</param>
    /// <param name="region">If not null this provides geographic information to pick the nearest database server.</param>
    /// <param name="version">Optional: provides the version name in case that effects the database selection</param>
    /// <returns>Status with the DatabaseInfoName, or error if it can't find a database to work with</returns>
    public async Task<IStatusGeneric<string>> FindOrCreateShardingEntryAsync(
        bool hasOwnDb, string createTimestamp, string region, string version = null)
    {
        var status = new StatusGenericLocalizer<string>(_localizeDefault);

        //This gets the databases with the info on whether the database is available
        var dbsWithUsers = await _shardingService.GetShardingsWithTenantNamesAsync();

        status.SetResult(hasOwnDb
            ? // this will find the first empty database
              dbsWithUsers
                .FirstOrDefault(x => x.hasOwnDb == null).shardingName
            : // this will find the first database that can be used for non-sharding tenants
            dbsWithUsers
                .Where(x => (x.hasOwnDb == null || x.hasOwnDb == false)
                            // This means there is a limit of 50 shared tenants in any one database
                            && x.tenantNames.Count < 50)
                //This puts the databases that can only contain shared databases first
                .OrderByDescending(x => x.hasOwnDb)
                //This then orders the database with least tenants first
                .ThenBy(x => x.tenantNames.Count)
                .FirstOrDefault().shardingName);

        if (status.Result == null)
            //This returns an error, but you could create a new database if none are available.
            status.AddErrorString("NoDbForTenant".ClassLocalizeKey(this, true),
                "We cannot create the tenant at this time. Please contact the support team with the code: no db available.");

        return status;
    }
}