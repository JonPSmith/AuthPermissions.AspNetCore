// Copyright (c) 2024 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Net.DistributedFileStoreCache;
using RunMethodsSequentially;

namespace AuthPermissions.AspNetCore.StartupServices;

/// <summary>
/// This service will run code once the application to made sure that the FileStore cache
/// is up-to-date. There are three possible situations
///
/// 1. NO ENTRIES IN BOTH FILESTORE CACHE AND THE SHARDINGBACKUP DATABASE
///    This happens if the application has no shardings. For instance, when you
///    first deploy of your multi-tenant application there are not shardings or
///    tenants.
/// 2. ENTRIES IN FILESTORE CACHE, BUT NO ENTRIES IN SHARDINGBACKUP DATABASE
///    This happens when you first add the "Backup your shardings" feature
///    by setting up a RegisterServiceToRunInJob to run this class on startup.
///    In this case this code will copy the shardings from FileStore Cache into
///    the ShardingBackup database to provide a backup of the FileStore Cache shardings. 
/// 3. HAD ENTRIES IN BOTH FILESTORE CACHE AND THE SHARDINGBACKUP DATABASE
///    This is the normal state, but we take the time on startup to check that
///    the FileStore Cache and ShardingBackup db contain the same sharding data. 
///    The tests are (NOTE: ever check error creates a LogError message) 
///    3.1. Check for missing sharding entries, e.g. a FileStore Cache sharding
///         exists, but there isn't a sharding entry in the ShardingBackup db.
///         This uses the "Name" property to compare sharding entries.
///    3.2. Checks that the two sharding entries with the same "Name" that
///         they have the same data in all of its properties.
///    If any of these tests fail, then its throw an exception to stop your application.
///    That's because you your tenant data aren't correct. See the AuthP documentation
///    called 'Backup your shardings' for more information.
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
    public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
    {
        var fsCache = scopedServices.GetRequiredService<IDistributedFileStoreCacheClass>();
        var authDbContext = scopedServices.GetRequiredService<AuthPermissionsDbContext>();
        var logger = scopedServices.GetRequiredService<ILogger<StartupServiceShardingBackup>>();
        var shardingEntryPrefix = GetSetShardingEntriesFileStoreCache.ShardingEntryPrefix;

        //-----------------------------------------------------------------------------------
        //SETUP

        //Get the ShardingEntries from the two ShardingEntry resources 
        var fsCacheShardings = fsCache.GetAllKeyValues()
            .Where(kv => kv.Key.StartsWith(shardingEntryPrefix))
            .Select(s => fsCache.GetClassFromString<ShardingEntry>(s.Value))
            .OrderBy(x => x.Name).ToList();
        var dbShardings = await authDbContext.ShardingEntryBackup.ToListAsync();

        //---------------------------------------------------------------------------
        //1. NO ENTRIES IN BOTH FILESTORE CACHE AND THE SHARDINGBACKUP DATABASE
        //OPERATION: do nothing, because there are no ShardingEntries, e.g. first deploy of the app.  
        if (!fsCacheShardings.Any() && !dbShardings.Any())
        {
            //No ShardingEntries in both FileStore and ShardingEntryBackup, so nothing to do
            //This happens when the application has no tenants.
            logger.LogInformation("No ShardingEntries in both FileStore and ShardingEntryBackup, " +
                                  "which means there isn't anything to do. " +
                "This case occurs when the app hasn't any tenants.");
            return;
        }

        //---------------------------------------------------------------------------
        //2. ENTRIES IN FILESTORE CACHE, BUT NO ENTRIES IN SHARDINGBACKUP DATABASE
        //OPERATION: the FileStore Cache is backed up into the ShardingEntryBackup database 
        //This happens the first time you add the StartupServiceShardingBackup and deploy your app. 
        if (fsCacheShardings.Any() && !dbShardings.Any())
        {
            logger.LogInformation("The FileStore Cache's ShardingEntries has entries, " +
                                  "but the ShardingBackup database is empty. This means that the FileStore Cache's " +
                                  "ShardingEntries entries are copied into the ShardingBackup database.");
            authDbContext.ShardingEntryBackup.AddRange(fsCacheShardings);
            await authDbContext.SaveChangesAsync();
            return;
        }

        //----------------------------------------------------------------------------
        //3. HAD ENTRIES IN BOTH FILESTORE CACHE AND THE SHARDINGBACKUP DATABASE
        //OPERATION: Normal state, but we take the time to check the two ShardingEntries sources match
        if (fsCacheShardings.Any() && dbShardings.Any())
        {
            var numErrors = 0;

            //3.1. Check for missing sharding entries, e.g. a FileStore Cache sharding
            var shardingBackupMissing = fsCacheShardings.Select(x => x.Name)
                .Except(dbShardings.Select(x => x.Name)).ToArray();
            foreach (var missingKey in shardingBackupMissing)
            {
                numErrors++;
                logger.LogError($"The ShardingBackup database is missing an entry with the Name of '{missingKey}'.");
            }
            var fsCacheMissing = dbShardings.Select(x => x.Name)
                .Except(fsCacheShardings.Select(x => x.Name)).ToArray();
            foreach (var missingKey in fsCacheMissing)
            {
                numErrors++;
                logger.LogError($"The FileStore Cache is missing an entry with the Name of '{missingKey}'.");
            }

            //3.2 Checks that the ShardingBackup db entries have the same data as the fsCacheShardings

            foreach (var fsCacheEntry in fsCacheShardings)
            {
                if (dbShardings.Exists(x => x.Name == fsCacheEntry.Name) &&
                    !dbShardings.Single(x => x.Name == fsCacheEntry.Name).Equals(fsCacheEntry))
                {
                    numErrors++;
                    logger.LogError($"The ShardingBackup database entry with the Name of '{fsCacheEntry.Name}' " +
                                    " does not match the FileStore Cache with the same Name.");
                }
            }

            if (numErrors == 0)
                logger.LogInformation(
                    $"Everything is correct: There are {fsCacheShardings.Count} ShardingEntries in the " +
                    "FileStore Cache which matches the ShardingEntries in the sharding backup database.");

            else
            {
                throw new InvalidOperationException(
                    $"The {nameof(ShardingEntry)} entries are either have " +
                    $"missing entries and / or some of the {nameof(ShardingEntry)} don't match the with backup versions. " +
                    $"See the 'Backup your shardings' section in the AuthP's Wiki for what to do if this happens.");
            }
        }
    }

}