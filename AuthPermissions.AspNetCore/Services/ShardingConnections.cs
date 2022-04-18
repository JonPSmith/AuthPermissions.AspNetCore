// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

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
    private readonly ConnectionStringsOption _connectionDict;
    private readonly ShardingSettingsOption _shardingSettings;
    private readonly AuthPermissionsDbContext _context;

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
        //If no shardingsetting.json file, then we provide one default sharding settings data
        _shardingSettings.ShardingDatabases ??= new List<ShardingDatabaseData>
        {
            new ShardingDatabaseData { Name = options.ShardingDefaultDatabaseInfoName }
        };
        
        _context = context;
    }

    /// <summary>
    /// This returns all the database names in the shardingsetting.json file
    /// See <see cref="ShardingSettingsOption"/> for the format of that file
    /// NOTE: If the shardingsetting.json file is missing, or there is no "ShardingData" section,
    /// then it will return one <see cref="ShardingSettingsOption"/> that uses the "DefaultConnection" connection string
    /// </summary>
    /// <returns>A list of <see cref="ShardingDatabaseData"/> from the shardingsetting.json file</returns>
    public List<ShardingDatabaseData> GetAllPossibleShardingData()
    {
        return _shardingSettings.ShardingDatabases;
    }

    /// <summary>
    /// This returns all the database info names in the shardingsetting.json file, with a list of tenant name linked to each connection name
    /// </summary>
    /// <returns>List of all the database info names with the tenants using that database data name</returns>
    public async Task<List<(string databaseInfoName, List<string> tenantNames)>> GetDatabaseInfoNamesWithTenantNamesAsync()
    {
        var nameAndConnectionName  = await _context.Tenants
            .Select(x => new { x.ConnectionName, x.TenantFullName})
            .ToListAsync();
            
        var grouped = nameAndConnectionName.GroupBy(x => x.ConnectionName)
            .ToDictionary(x => x.Key,
                y => y.Select(z => z.TenantFullName));

        var result = new List<(string connectionName, List<string>)>();
        //Add sharding database names that have no tenants in them so that you can see all the connection string  names
        foreach (var databaseInfoName in _shardingSettings.ShardingDatabases.Select(x => x.Name))
        {
            result.Add(grouped.ContainsKey(databaseInfoName)
                ? (databaseInfoName, grouped[databaseInfoName].ToList())
                : (databaseInfoName, new List<string>()));
        }

        return result;
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

    //-----------------------------------------------------
    // private methods

    /// <summary>
    /// This changes the database to the <see cref="ShardingDatabaseData.DatabaseName"/> in the given connectionString
    /// NOTE: If the <see cref="ShardingDatabaseData.DatabaseName"/> is null, then it returns the connectionString with no change
    /// </summary>
    /// <param name="databaseData">Information about the database type/name to be used in the connection string</param>
    /// <param name="connectionString"></param>
    /// <returns>A connection string containing the correct database to be used</returns>
    /// <exception cref="InvalidEnumArgumentException"></exception>
    private static string SetDatabaseInConnectionString(ShardingDatabaseData databaseData, string connectionString)
    {
        if (databaseData.DatabaseName == null)
            //This uses the database that is already in the connection string
            return connectionString;

        switch (databaseData.DatabaseType)
        {
            case "SqlServer":
            {
                var builder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = databaseData.DatabaseName
                };
                return builder.ConnectionString;
            }
            case "Postgres":
            {
                var builder = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Database = databaseData.DatabaseName
                };
                return builder.ConnectionString;
            }
            default:
                throw new InvalidEnumArgumentException(
                    $"Missing a switch to handle a database type of {databaseData.DatabaseType}");
        }
    }
}