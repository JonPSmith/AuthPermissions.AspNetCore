// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.Services;

/// <summary>
/// This is used to get all the connection strings in the appsettings file
/// </summary>
public class ConnectionStringsOption : Dictionary<string, string> { }

/// <summary>
/// This service manages access to databases when <see cref="TenantTypes.AddSharding"/> is turned on
/// </summary>
public class ShardingConnections : IShardingConnections
{
    /// <summary>
    /// Defines the string for a SQL Server database server
    /// </summary>
    public const string SqlServerType = "SqlServer";
    /// <summary>
    /// Defines the string for a PostgreSQL database server
    /// </summary>
    public const string PostgresType =  "Postgres";

    private readonly ConnectionStringsOption _connectionDict;
    private readonly ShardingSettingsOption _shardingSettings;
    private readonly AuthPermissionsDbContext _context;
    private readonly AuthPermissionsOptions _options;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="connectionsAccessor">Dynamically assesses the ConnectionStings part of the appsetting file</param>
    /// <param name="shardingSettingsAccessor">Dynamically assesses the ShardingData part of the shardingsetting file</param>
    /// <param name="context">AuthP context - used in <see cref="GetDatabaseInfoNamesWithTenantNamesAsync"/> method</param>
    /// <param name="options"></param>
    public ShardingConnections(IOptionsSnapshot<ConnectionStringsOption> connectionsAccessor,
        IOptionsSnapshot<ShardingSettingsOption> shardingSettingsAccessor,
        AuthPermissionsDbContext context, AuthPermissionsOptions options)
    {
        //thanks to https://stackoverflow.com/questions/37287427/get-multiple-connection-strings-in-appsettings-json-without-ef
        _connectionDict = connectionsAccessor.Value;
        _shardingSettings = shardingSettingsAccessor.Value;
        _context = context;
        _options = options;

        //If no shardingsetting.json file, then we provide one default sharding settings data
        //which also contains other support data
        _shardingSettings.ShardingDatabases ??= new List<DatabaseInformation>
        {
            new DatabaseInformation { Name = options.ShardingDefaultDatabaseInfoName }
        };
    }

    /// <summary>
    /// This returns all the database names in the shardingsetting.json file
    /// See <see cref="ShardingSettingsOption"/> for the format of that file
    /// NOTE: If the shardingsetting.json file is missing, or there is no "ShardingData" section,
    /// then it will return one <see cref="ShardingSettingsOption"/> that uses the "DefaultConnection" connection string
    /// </summary>
    /// <returns>A list of <see cref="DatabaseInformation"/> from the shardingsetting.json file</returns>
    public List<DatabaseInformation> GetAllPossibleShardingData()
    {
        return _shardingSettings.ShardingDatabases;
    }

    /// <summary>
    /// This provides the names of the connection string
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetConnectionStringNames()
    {
        return _connectionDict.Keys;
    }

    /// <summary>
    /// This returns all the database info names in the shardingsetting.json file, with a list of tenant name linked to each connection name
    /// NOTE: The DatabaseInfoName which matches the <see cref="AuthPermissionsOptions.ShardingDefaultDatabaseInfoName"/> is always
    /// returns a HasOwnDb value of false. This is because the default database has the AuthP data in it.
    /// </summary>
    /// <returns>List of all the database info names with the tenants using that database data name
    /// NOTE: The hasOwnDb is true for a database containing a single database, false for multiple tenant database and null if empty</returns>
    public async Task<List<(string databaseInfoName, bool? hasOwnDb, List<string> tenantNames)>> GetDatabaseInfoNamesWithTenantNamesAsync()
    {
        var nameAndConnectionName  = await _context.Tenants
            .Select(x => new { ConnectionName = x.DatabaseInfoName, x})
            .ToListAsync();
            
        var grouped = nameAndConnectionName.GroupBy(x => x.ConnectionName)
            .ToDictionary(x => x.Key,
                y => y.Select(z => new {z.x.HasOwnDb, z.x.TenantFullName}));

        var result = new List<(string databaseInfoName, bool? hasOwnDb, List<string>)>();
        //Add sharding database names that have no tenants in them so that you can see all the connection string  names
        foreach (var databaseInfoName in _shardingSettings.ShardingDatabases.Select(x => x.Name))
        {
            result.Add(grouped.ContainsKey(databaseInfoName)
                ? (databaseInfoName,
                    databaseInfoName == _options.ShardingDefaultDatabaseInfoName
                        ? false //The default DatabaseInfoName contains the AuthP information, so its a shared database
                        : grouped[databaseInfoName].FirstOrDefault()?.HasOwnDb,  
                    grouped[databaseInfoName].Select(x => x.TenantFullName).ToList())
                : (databaseInfoName, 
                    databaseInfoName == _options.ShardingDefaultDatabaseInfoName ? false : null,
                    new List<string>()));
        }

        return result;
    }

    /// <summary>
    /// This returns a list of the DatabaseType supported by this implementation of the <see cref="IShardingConnections"/>
    /// </summary>
    /// <returns>The strings defining the different database types that are supported</returns>
    public string[] GetSupportedDatabaseTypes()
    {
        return new string []{ SqlServerType, PostgresType};
    }

    /// <summary>
    /// This will provide the connection string for the entry with the given database info name
    /// </summary>
    /// <param name="databaseInfoName">The name of sharding database info we want to access</param>
    /// <returns>The connection string, or null if not found</returns>
    public string FormConnectionString(string databaseInfoName)
    {
        if (databaseInfoName == null)
            throw new AuthPermissionsException("The name of the database date can't be null");

        var databaseData = _shardingSettings.ShardingDatabases.SingleOrDefault(x => x.Name == databaseInfoName);
        if (databaseData == null)
            return null;

        if (!_connectionDict.TryGetValue(databaseData.ConnectionName, out var connectionString))
            throw new AuthPermissionsException(
                $"Could not find the connection name '{connectionString}' that the sharding database data '{databaseInfoName}' requires.");

        return SetDatabaseInConnectionString(databaseData, connectionString);
    }

    /// <summary>
    /// This method allows you to check that the <see cref="DatabaseInformation"/> will create a
    /// a valid connection string. Useful when adding or editing the data in the shardingsettings file.
    /// </summary>
    /// <param name="databaseInfo">The full definition of the <see cref="DatabaseInformation"/> for this database info</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IStatusGeneric TestFormingConnectionString(DatabaseInformation databaseInfo)
    {
        var status = new StatusGenericHandler();

        if (databaseInfo == null)
            throw new ArgumentNullException(nameof(databaseInfo));

        if (!_connectionDict.TryGetValue(databaseInfo.ConnectionName, out var connectionString))
            return status.AddError(
                $"The {nameof(DatabaseInformation.ConnectionName)} '{databaseInfo.ConnectionName}' " +
                "wasn't found in the connection strings.");
        try
        {
            SetDatabaseInConnectionString(databaseInfo, connectionString);
        }
        catch (AuthPermissionsException e)
        {
            status.AddError(e.Message);
        }
        catch
        {
            status.AddError(
                "There was an  error when trying to create a connection string. Typically this is because " +
                $"the connection string doesn't match the {nameof(DatabaseInformation.DatabaseType)}.");
        }

        return status;
    }

    //-----------------------------------------------------
    // private methods

    /// <summary>
    /// This changes the database to the <see cref="DatabaseInformation.DatabaseName"/> in the given connectionString
    /// NOTE: If the <see cref="DatabaseInformation.DatabaseName"/> is null / empty, then it returns the connectionString with no change
    /// </summary>
    /// <param name="databaseInformation">Information about the database type/name to be used in the connection string</param>
    /// <param name="connectionString"></param>
    /// <returns>A connection string containing the correct database to be used</returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    private static string SetDatabaseInConnectionString(DatabaseInformation databaseInformation, string connectionString)
    {
        switch (databaseInformation.DatabaseType)
        {
            case SqlServerType:
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                if (string.IsNullOrEmpty(builder.InitialCatalog) && string.IsNullOrEmpty(databaseInformation.DatabaseName))
                    throw new AuthPermissionsException(
                        $"The {nameof(DatabaseInformation.DatabaseName)} can't be null or empty " +
                        "when the connection string doesn't have a database defined.");

                if (string.IsNullOrEmpty(databaseInformation.DatabaseName))
                    //This uses the database that is already in the connection string
                    return connectionString;
                builder.InitialCatalog = databaseInformation.DatabaseName;
                return builder.ConnectionString;
            }
            case PostgresType:
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString);
                if (string.IsNullOrEmpty(builder.Database) && string.IsNullOrEmpty(databaseInformation.DatabaseName))
                    throw new AuthPermissionsException(
                        $"The {nameof(DatabaseInformation.DatabaseName)} can't be null or empty " +
                        "when the connection string doesn't have a database defined.");

                if (string.IsNullOrEmpty(databaseInformation.DatabaseName))
                    //This uses the database that is already in the connection string
                    return connectionString;

                builder.Database = databaseInformation.DatabaseName;
                return builder.ConnectionString;
            }
            default:
                throw new InvalidEnumArgumentException(
                    $"Missing a switch to handle a database type of {databaseInformation.DatabaseType}");
        }
    }
}