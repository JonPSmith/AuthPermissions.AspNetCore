// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using StatusGeneric;

namespace AuthPermissions.SupportCode;

/// <summary>
/// This is a demo implementation of the <see cref="IGetDatabaseForNewTenant"/> interface
/// </summary>
public class DemoGetDatabaseForNewTenant : IGetDatabaseForNewTenant
{
    private readonly IGetSetShardingEntries _shardingService;
    private readonly AuthPermissionsDbContext _context;
    private readonly IDefaultLocalizer _localizeDefault;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="shardingService"></param>
    /// <param name="context"></param>
    /// <param name="localizeProvider"></param>
    public DemoGetDatabaseForNewTenant(IGetSetShardingEntries shardingService,
        AuthPermissionsDbContext context,
        IAuthPDefaultLocalizer localizeProvider)
    {
        _shardingService = shardingService;
        _context = context;
        _localizeDefault = localizeProvider.DefaultLocalizer;
    }

    /// <summary>
    /// This will look for a database for a new tenant.
    /// If the hasOwnDb is true, then it will find an empty database,
    /// otherwise it will look for database containing multiple tenants
    /// </summary>
    /// <param name="tenant">This is the tenant that you want to find/create a new database.
    /// NOTE: The tenant hasn't been written to the database at this stage, so the TenantId is zero.</param>
    /// <param name="hasOwnDb">If true the tenant needs its own database. False means it shares a database.</param>
    /// <param name="region">If not null this provides geographic information to pick the nearest database server.</param>
    /// <param name="version">Optional: provides the version name in case that effects the database selection</param>
    /// <returns>Status with the DatabaseInfoName, or error if it can't find a database to work with</returns>
    public async Task<IStatusGeneric<Tenant>> FindOrCreateDatabaseAsync(Tenant tenant, bool hasOwnDb, string region,
        string version = null)
    {
        var status = new StatusGenericLocalizer<Tenant>(_localizeDefault);

        //This gets the databases with the info on whether the database is available
        var dbsWithUsers = await _shardingService.GetShardingsWithTenantNamesAsync();

        var foundDatabaseInfoName = hasOwnDb
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
                .FirstOrDefault().shardingName;

        if (foundDatabaseInfoName == null)
            //This returns an error, but you could create a new database if none are available.
            status.AddErrorString("NoDbForTenant".ClassLocalizeKey(this, true),
                "We cannot create the tenant at this time. Please contact the support team with the code: no db available.");

        //Now set up the sharding parts of the tenant
        tenant.UpdateShardingState(foundDatabaseInfoName, hasOwnDb);
        status.CombineStatuses(await _context.SaveChangesWithChecksAsync(_localizeDefault));

        return status.SetResult(tenant);
    }

    /// <summary>
    /// If called it will undo what the <see cref="IGetDatabaseForNewTenant.FindOrCreateDatabaseAsync"/> did.
    /// This is called if there was a problem with the new user such that the new tenant would be removed.
    /// </summary>
    /// <returns></returns>
    public Task<IStatusGeneric> RemoveLastDatabaseSetupAsync()
    {
        //This doesn't do anything as this service doesn't create new database, it only looks for an existing database.
        return  Task.FromResult<IStatusGeneric>(new StatusGenericLocalizer(_localizeDefault));
    }
}