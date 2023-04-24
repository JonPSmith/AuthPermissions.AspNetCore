// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This contains sharding methods that are specific database providers.
/// There are two build-in versions for SqlServer and Postgres, which as built in.
/// If the developer wants to use a different database provider (referred as "CustomDatabase"),
/// then they need to provide a version with the correct 
/// </summary>
public interface IDatabaseSpecificMethods
{
    /// <summary>
    /// This contains the short name of EF Core Database Provider that this service supports
    /// e.g. "SqlServer" instead of "Microsoft.EntityFrameworkCore.SqlServer"  
    /// </summary>
    public string DatabaseProviderShortName { get; }

    /// <summary>
    /// This changes the database to the <see cref="DatabaseInformation.DatabaseName"/> in the given connectionString
    /// NOTE: If the <see cref="DatabaseInformation.DatabaseName"/> is null / empty, then it returns the connectionString with no change
    /// </summary>
    /// <param name="databaseInformation">Information about the database type/name to be used in the connection string</param>
    /// <param name="connectionString"></param>
    /// <returns>A connection string containing the correct database to be used, or errors</returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    public IStatusGeneric<string> SetDatabaseInConnectionString(DatabaseInformation databaseInformation,
        string connectionString);

    /// <summary>
    /// This will execute the function (which updates the shardingsettings json file) within a Distributed Lock. 
    /// Typically this will use a lock on the database provider.  
    /// </summary>
    /// <param name="connectionString">connection string to the database to place a Distributed Lock on</param>
    /// <param name="runInLock"></param>
    /// <returns></returns>
    public IStatusGeneric ChangeDatabaseInformationWithinDistributedLock(string connectionString,
        Func<IStatusGeneric> runInLock);

    /// <summary>
    /// This will execute the function (which updates the shardingsettings json file) within a Distributed Lock. 
    /// Typically this will use a lock on the database provider.  
    /// </summary>
    /// <param name="connectionString">connection string to the database to place a Distributed Lock on</param>
    /// <param name="runInLockAsync"></param>
    /// <returns></returns>
    public Task<IStatusGeneric> ChangeDatabaseInformationWithinDistributedLockAsync(string connectionString,
        Func<Task<IStatusGeneric>> runInLockAsync);
}