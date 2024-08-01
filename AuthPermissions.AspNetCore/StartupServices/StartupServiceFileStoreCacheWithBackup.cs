// Copyright (c) 2024 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.DistributedFileStoreCache;
using RunMethodsSequentially;

namespace AuthPermissions.AspNetCore.StartupServices;

/// <summary>
/// This service will run code once the application to made sure that the FileStore cache
/// is up-to-date. There are three possible situations
///
/// 1. NORMAL: FileStore cache and ShardingEntryBackup both have entries.
///    In this case it compares the two ShardingEntry and usually there should match,
///    but if they are differences between the FileStore cache and ShardingEntryBackup
///    it throws an exception. See the documentation about what to do in this case.
/// 2. FIRST USE OF VERSION 8.1.0: The ShardingEntryBackup database will be empty.
///    The first time you update to version 8.1.0, and you added this StartupService the
///    ShardingEntryBackup database will be empty. In that case it will copy FileStore cache's 
///    <see cref="ShardingEntry"/> into the ShardingEntryBackup database. 
/// 3. FILESTORE CACHE FILE HAS BEEN DELETED: Recovers the sharding from the ShardingEntryBackup db. 
///    If the FileStore cache is empty (i.e.no <see cref="ShardingEntry"/> entries) and the
///    ShardingEntryBackup database does have <see cref="ShardingEntry"/> entries, then it will
///    copy any see cref="ShardingEntry"/> from ShardingEntryBackup database into the FileStore cache.
/// </summary>
public class StartupServiceShardingBackup() : IStartupServiceToRunSequentially
{
    /// <summary>
    /// Set to -3 so that this startup service runs after the migrations
    /// </summary>
    public int OrderNum { get; } = -3;

    /// <summary>
    /// This method registers FileStore Cache and then checks that the FileStore Cache has the correct
    /// <see cref="ShardingEntry"/>'s in it. See the list of the three 
    /// in the FileStore Cache are OK. Then it checks the FileStore Cache and if it has been accidentally deleted,
    /// then it will refill the FileStore Cache from the ShardingEntryBackup database.
    /// See the Class comment for the full details of the scenarios.
    /// </summary>
    /// <param name="scopedServices">The RunMethodsSequentially JobRunner will provide a scoped service provider,
    /// which allows you to obtain services more efficiently, but be aware the scoped service is used by every
    /// startup services you register.
    /// </param>
    /// <returns></returns>
    public ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
    {
        var fsCache = scopedServices.GetRequiredService<IDistributedFileStoreCacheClass>();
        var authDbContext = scopedServices.GetRequiredService<AuthPermissionsDbContext>();
        var authPermissionsOptions = scopedServices.GetRequiredService<AuthPermissionsOptions>();
        var shardingEntryOptions = scopedServices.GetRequiredService<ShardingEntryOptions>();
        var logger = scopedServices.GetRequiredService<ILogger<StartupServiceShardingBackup>>();
        var shardingEntryPrefix = GetSetShardingEntriesFileStoreCache.ShardingEntryPrefix;

        var nameOfHybridDefault =  authPermissionsOptions.DefaultShardingEntryName;

        //Get the ShardingEntries from the two ShardingEntry resources 
        var fsCacheShardings = fsCache.GetAllKeyValues()
            .Where(kv => kv.Key.StartsWith(shardingEntryPrefix))
            .Select(s => fsCache.GetClassFromString<ShardingEntry>(s.Value))
            .OrderBy(x => x.Name).ToList();
        if (shardingEntryOptions.HybridMode)
        {
            //we need to remove the HybridDefault form the list of sharding
            //because the ShardingEntryBackup doesn't have that entry.
            var hybridEntry = fsCacheShardings.SingleOrDefault(x => x.Name == nameOfHybridDefault);
            if (hybridEntry != null)
                ((IList)fsCacheShardings).Remove(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(hybridEntry.Name));
        }
        var dbShardings = authDbContext.ShardingEntryBackup.ToList();

        if (!fsCacheShardings.Any() && !dbShardings.Any())
        {
            //No shardings in FileStore and ShardingEntryBackup, so nothing to do
            //This happens when the application has no tenants.
            logger.LogInformation("No sharding entries were found in the FileStore Cache or ShardingEntryBackup database ."+
                "That's means no tenants are setup in this application.");
            return ValueTask.CompletedTask;
        }

        if (!fsCacheShardings.Any())
        {
            //This is scenario "3. FILESTORE CACHE FILE HAS BEEN DELETED"
            //In this case we copy the sharings in the ShardingEntryBackup database into to the FileStore Cache

            foreach (var backupSharding in dbShardings)
            {
                fsCache.SetClass(GetSetShardingEntriesFileStoreCache.FormShardingEntryKey(backupSharding.Name), backupSharding);
            }
            logger.LogError("The FileStore Cache has been deleted of shardings and this service copied " +
                                  $"{dbShardings.Count} shardings from the ShardingEntryBackup database.");
        }
        else
        {
            //This is  "1. NORMAL" scenario, where we cross-check the two sharding resources.
            //The likelihood of there the sharding resources are difference is very small,
            //so we look for missing ShardingEntry's by comparing the "Name" property. 

            var fsCacheShardingsName = dbShardings.Select(x => x.Name).ToArray();
            var dbShardingsNames = dbShardings.Select(x => x.Name).ToArray();
            
            //Check that the FileStore Cache isn't missing a sharding
            foreach (var name in dbShardingsNames)
            {
                if (!fsCacheShardingsName.Contains(name))
                    //The ShardingEntryBackup doesn't have the sharding
                    logger.LogError($"The FileStore Cache is missing an entry with the Name of '{name}.");
            }

            //Check that the ShardingEntryBackup database isn't missing a sharding
            foreach (var name in fsCacheShardingsName)
            {
                if (!dbShardingsNames.Contains(name))
                    //The ShardingEntryBackup doesn't have the sharding
                    logger.LogError($"The shardingEntryBackup database is missing an entry with the Name of '{name}.");
            }
        }

        return ValueTask.CompletedTask;
    }

}