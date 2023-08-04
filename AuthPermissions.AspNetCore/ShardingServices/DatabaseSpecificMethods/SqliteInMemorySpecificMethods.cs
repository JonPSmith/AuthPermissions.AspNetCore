// // Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// // Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;
using Medallion.Threading.FileSystem;
using Microsoft.Data.Sqlite;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices.DatabaseSpecificMethods;

/// <summary>
/// This class implements the sharding methods for a Sqlite database.
/// i.e. it provides it provides Sqlite methods for creating connection strings.
/// Your would register this class to the DI in your custom database extension methods
/// </summary>
public class SqliteInMemorySpecificMethods : IDatabaseSpecificMethods
{
    private readonly AuthPermissionsOptions _options;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="options"></param>
    public SqliteInMemorySpecificMethods(AuthPermissionsOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// This is used select the <see cref="IDatabaseSpecificMethods"/> from the AuthP's <see cref="SetupInternalData.AuthPDatabaseType"/>
    /// </summary>
    public AuthPDatabaseTypes AuthPDatabaseType => AuthPDatabaseTypes.SqliteInMemory;

    /// <summary>
    /// <summary>
    /// This contains the short name of EF Core Database Provider that this service supports
    /// e.g. "SqlServer" instead of "Microsoft.EntityFrameworkCore.SqlServer"
    /// Useful when use showing database type to a user and used internal
    /// NOTE: The name MUST contain the last part of the DbContext.Database.ProviderName , e.g. PostgreSQL
    /// </summary>
    /// </summary>
    public string DatabaseProviderShortName => "Sqlite";

    /// <summary>
    /// This simply returns a in-memory connection string
    /// </summary>
    /// <param name="shardingEntry"></param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public string FormShardingConnectionString(ShardingEntry shardingEntry, string connectionString)
    {
        return new SqliteConnectionStringBuilder { DataSource = ":memory:" }.ConnectionString;
    }

    /// <summary>
    /// This will execute the function (which updates the shardingsettings json file) within a Distributed Lock. 
    /// Typically this will use a lock on the database provider.  
    /// </summary>
    /// <param name="connectionString">connection string to the database to place a Distributed Lock on</param>
    /// <param name="runInLock"></param>
    /// <returns></returns>
    public IStatusGeneric ChangeDatabaseInformationWithinDistributedLock(string connectionString, Func<IStatusGeneric> runInLock)
    {
        //The https://github.com/madelson/DistributedLock doesn't support Sqlite for locking
        //so we just use the File lock
        //NOTE: DistributedLock does support many database types and its fairly easy to build a LockAndRun method
        var myDistributedLock = new FileDistributedLock(GetDirectoryInfoToLockWithCheck(), "MyLockName");
        using (myDistributedLock.Acquire())
        {
            return runInLock();
        }
    }

    /// <summary>
    /// This will execute the function (which updates the shardingsettings json file) within a Distributed Lock. 
    /// Typically this will use a lock on the database provider.  
    /// </summary>
    /// <param name="connectionString">connection string to the database to place a Distributed Lock on</param>
    /// <param name="runInLockAsync"></param>
    /// <returns></returns>
    public async Task<IStatusGeneric> ChangeDatabaseInformationWithinDistributedLockAsync(string connectionString,
        Func<Task<IStatusGeneric>> runInLockAsync)
    {
        //The https://github.com/madelson/DistributedLock doesn't support Sqlite for locking
        //so we just use the File lock
        //NOTE: DistributedLock does support many database types and its fairly easy to build a LockAndRun method
        var myDistributedLock = new FileDistributedLock(GetDirectoryInfoToLockWithCheck(), "MyLockName");
        using (await myDistributedLock.AcquireAsync())
        {
            return await runInLockAsync.Invoke();
        }
    }

    private DirectoryInfo GetDirectoryInfoToLockWithCheck()
    {
        if (string.IsNullOrEmpty(_options.PathToFolderToLock))
            throw new AuthPermissionsBadDataException(
                $"The {nameof(AuthPermissionsOptions.PathToFolderToLock)} property in the {nameof(AuthPermissionsOptions)}" +
                " must be set to a directory that all the instances of your application can access.");

        return new DirectoryInfo(_options.PathToFolderToLock);
    }
}