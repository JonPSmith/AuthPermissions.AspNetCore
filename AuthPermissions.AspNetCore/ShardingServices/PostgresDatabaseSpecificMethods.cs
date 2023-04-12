// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using Medallion.Threading.Postgres;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This contains the Postgres-specific sharding functions
/// </summary>
public class PostgresDatabaseSpecificMethods : IDatabaseSpecificMethods
{
    private readonly IAuthPDefaultLocalizer _localizeDefault;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="localizeDefault"></param>
    public PostgresDatabaseSpecificMethods(IAuthPDefaultLocalizer localizeDefault)
    {
        _localizeDefault = localizeDefault;
    }


    /// <summary>
    /// This contains the type of Database Provider the service supports
    /// </summary>
    public AuthPDatabaseTypes DatabaseProviderType => AuthPDatabaseTypes.Postgres;

    /// <summary>
    /// This changes the database to the <see cref="DatabaseInformation.DatabaseName"/> in the given connectionString
    /// NOTE: If the <see cref="DatabaseInformation.DatabaseName"/> is null / empty, then it returns the connectionString with no change
    /// </summary>
    /// <param name="databaseInformation">Information about the database type/name to be used in the connection string</param>
    /// <param name="connectionString"></param>
    /// <returns>A connection string containing the correct database to be used, or errors</returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    public IStatusGeneric<string> SetDatabaseInConnectionString(DatabaseInformation databaseInformation, string connectionString)
    {
        var status = new StatusGenericLocalizer<string>(_localizeDefault.DefaultLocalizer);

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrEmpty(builder.Database) && string.IsNullOrEmpty(databaseInformation.DatabaseName))
            throw new AuthPermissionsException(
                $"The {nameof(DatabaseInformation.DatabaseName)} can't be null or empty " +
                "when the connection string doesn't have a database defined.");

        if (string.IsNullOrEmpty(databaseInformation.DatabaseName))
            //This uses the database that is already in the connection string
            return status.SetResult(connectionString);

        builder.Database = databaseInformation.DatabaseName;
        return status.SetResult(builder.ConnectionString);
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