// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using Medallion.Threading.SqlServer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This contains the SqlServer-specific sharding functions
/// </summary>
public class SqlServerDatabaseSpecificMethods : IDatabaseSpecificMethods
{
    private readonly IAuthPDefaultLocalizer _localizeDefault;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="localizeDefault"></param>
    public SqlServerDatabaseSpecificMethods(IAuthPDefaultLocalizer localizeDefault)
    {
        _localizeDefault = localizeDefault;
    }

    /// <summary>
    /// This contains the type of Database Provider the service supports
    /// </summary>
    public AuthPDatabaseTypes DatabaseProviderType => AuthPDatabaseTypes.SqlServer;

    /// <summary>
    /// This changes the database to the <see cref="DatabaseInformation.DatabaseName"/> in the given connectionString
    /// NOTE: If the <see cref="DatabaseInformation.DatabaseName"/> is null / empty, then it returns the connectionString with no change
    /// </summary>
    /// <param name="databaseInformation">Information about the database type/name to be used in the connection string</param>
    /// <param name="connectionString">connection string to the database to place a Distributed Lock on</param>
    /// <returns>A connection string containing the correct database to be used, or errors</returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    public IStatusGeneric<string> SetDatabaseInConnectionString(DatabaseInformation databaseInformation, string connectionString)
    {
        var status = new StatusGenericLocalizer<string>(_localizeDefault.DefaultLocalizer);

        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrEmpty(builder.InitialCatalog) && string.IsNullOrEmpty(databaseInformation.DatabaseName))
            return status.AddErrorString("NoDatabaseDefined".ClassLocalizeKey(this, true),
                $"The {nameof(DatabaseInformation.DatabaseName)} can't be null or empty " +
                "when the connection string doesn't have a database defined.");

        if (string.IsNullOrEmpty(databaseInformation.DatabaseName))
            //This uses the database that is already in the connection string
            return status.SetResult(connectionString);

        builder.InitialCatalog = databaseInformation.DatabaseName;
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
        var myDistributedLock = new SqlDistributedLock("Sharding!", connectionString);
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
        var myDistributedLock = new SqlDistributedLock("Sharding!", connectionString);
        using (await myDistributedLock.AcquireAsync())
        {
            return await runInLockAsync();
        }
    }
}