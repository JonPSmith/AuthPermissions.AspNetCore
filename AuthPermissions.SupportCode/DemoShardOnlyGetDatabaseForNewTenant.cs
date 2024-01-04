// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using StatusGeneric;

namespace AuthPermissions.SupportCode;

/// <summary>
/// This contains a version of the <see cref="ISignUpGetShardingEntry"/> to handle tenants that have
/// sharding-Only tenants (i.e. the tenant's <see cref="Tenant.HasOwnDb"/> is true).
/// This means you need to create a new <see cref="ShardingEntry"/> for each new tenant
/// </summary>
public class DemoShardOnlyGetDatabaseForNewTenant : ISignUpGetShardingEntry
{
    private readonly IGetSetShardingEntries _accessShardingInfo;
    private readonly IDefaultLocalizer _localizeDefault;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="accessShardingInfo"></param>
    /// <param name="localizeProvider"></param>
    public DemoShardOnlyGetDatabaseForNewTenant(
        IGetSetShardingEntries accessShardingInfo, IAuthPDefaultLocalizer localizeProvider)
    {
        _accessShardingInfo = accessShardingInfo ?? throw new ArgumentNullException(nameof(accessShardingInfo));
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

        if (!hasOwnDb)
            status.AddErrorString("HasOwnDbBad".ClassLocalizeKey(this, true),
                "The HasOwnDb must be true as each tenant has there own database.");

        if (region == null)
            throw new AuthPermissionsBadDataException
                ("This example requires a region to have the name of the connection string to use");

        //First we create the sharding information or this new 
        var shardingEntry = new ShardingEntry
        {
            Name = $"SignOn-{createTimestamp}",
            ConnectionName = region,
            DatabaseName = $"Db-{createTimestamp}",
            DatabaseType = "SqlServer"
        };

        //This adds a new ShardingEntry to the sharding entries
        if (status.CombineStatuses(_accessShardingInfo.AddNewShardingEntry(shardingEntry))
            .HasErrors)
            return status; ;

        return status.SetResult(shardingEntry.Name);
    }
}