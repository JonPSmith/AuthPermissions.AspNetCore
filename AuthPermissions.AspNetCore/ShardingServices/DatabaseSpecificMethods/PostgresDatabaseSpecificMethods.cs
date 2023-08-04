// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;
using Medallion.Threading.Postgres;
using Npgsql;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices.DatabaseSpecificMethods;

/// <summary>
/// This contains the Postgres-specific sharding functions
/// </summary>
public class PostgresDatabaseSpecificMethods : IDatabaseSpecificMethods
{
    /// <summary>
    /// This is used select the <see cref="IDatabaseSpecificMethods"/> from the AuthP's <see cref="SetupInternalData.AuthPDatabaseType"/>
    /// </summary>
    public AuthPDatabaseTypes AuthPDatabaseType => AuthPDatabaseTypes.PostgreSQL;

    /// <summary>
    /// This contains the short name of EF Core Database Provider that this service supports
    /// e.g. "SqlServer" instead of "Microsoft.EntityFrameworkCore.SqlServer"
    /// Useful when use showing database type to a user and used internal
    /// NOTE: The name MUST contain the last part of the DbContext.Database.ProviderName , e.g. PostgreSQL
    /// </summary>
    public string DatabaseProviderShortName => "PostgreSQL";

    /// <summary>
    /// This changes the database to the <see cref="ShardingEntry.DatabaseName"/> in the given connectionString
    /// NOTE: If the <see cref="ShardingEntry.DatabaseName"/> is null / empty, then it returns the connectionString with no change
    /// </summary>
    /// <param name="shardingEntry">Information about the database type/name to be used in the connection string</param>
    /// <param name="connectionString"></param>
    /// <returns>A connection string containing the correct database to be used, or errors</returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    public string FormShardingConnectionString(ShardingEntry shardingEntry, string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrEmpty(builder.Database) && string.IsNullOrEmpty(shardingEntry.DatabaseName))
            throw new AuthPermissionsException(
                $"The {nameof(ShardingEntry.DatabaseName)} can't be null or empty " +
                "when the connection string doesn't have a database defined.");

        if (string.IsNullOrEmpty(shardingEntry.DatabaseName))
            //This uses the database that is already in the connection string
            return connectionString;

        builder.Database = shardingEntry.DatabaseName;
        return builder.ConnectionString;
    }

    /// <summary>
    /// This will execute the function (which updates the shardingsettings json file) within a Distributed Lock. 
    /// Typically this will use a lock on the database provider.  
    /// </summary>
    /// <param name="connectionString">connection string to the database to place a Distributed Lock on</param>
    /// <param name="runInLock"></param>
    /// <returns></returns>
    public IStatusGeneric ChangeDatabaseInformationWithinDistributedLock(string connectionString,
        Func<IStatusGeneric> runInLock)
    {
        //See this as to why the name is 9 digits long https://github.com/madelson/DistributedLock/blob/master/docs/DistributedLock.Postgres.md#implementation-notes
        var myDistributedLock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("Sharding!", allowHashing: true),
            connectionString);
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
        //See this as to why the name is 9 digits long https://github.com/madelson/DistributedLock/blob/master/docs/DistributedLock.Postgres.md#implementation-notes
        var myDistributedLock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("Sharding!", allowHashing: true),
            connectionString);
        using (await myDistributedLock.AcquireAsync())
        {
            return await runInLockAsync();
        }
    }
}