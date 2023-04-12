// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This is used to get all the connection strings in the appsettings file
/// </summary>
public class ConnectionStringsOption : Dictionary<string, string> {}

/// <summary>
/// This service manages access to databases when <see cref="TenantTypes.AddSharding"/> is turned on
/// </summary>
public class ShardingConnections : IShardingConnections
{
    private readonly ConnectionStringsOption _connectionDict;
    private readonly AuthPermissionsDbContext _context;
    private readonly IDefaultLocalizer _localizeDefault;
    private readonly AuthPermissionsOptions _options;
    private readonly ShardingSettingsOption _shardingSettings;

    /// <summary>
    /// This contains the methods with are specific to a database provider
    /// </summary>
    public IReadOnlyDictionary<string, IDatabaseSpecificMethods> DatabaseProviderMethods { get; }

    /// <summary>
    /// This returns the supported database provider that can be used for multi tenant sharding
    /// </summary>
    public string[] SupportedDatabaseProviders => DatabaseProviderMethods.Keys.ToArray();

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="connectionsAccessor">Dynamically assesses the ConnectionStings part of the appsetting file</param>
    /// <param name="shardingSettingsAccessor">Dynamically assesses the ShardingData part of the shardingsetting file</param>
    /// <param name="context">AuthP context - used in <see cref="GetDatabaseInfoNamesWithTenantNamesAsync"/> method</param>
    /// <param name="options"></param>
    /// <param name="databaseProviderMethods"></param>
    /// <param name="localizeProvider">Provides the used to localize any errors or messages</param>
    public ShardingConnections(IOptionsSnapshot<ConnectionStringsOption> connectionsAccessor,
        IOptionsSnapshot<ShardingSettingsOption> shardingSettingsAccessor,
        AuthPermissionsDbContext context, AuthPermissionsOptions options, 
        IEnumerable<IDatabaseSpecificMethods> databaseProviderMethods,
        IAuthPDefaultLocalizer localizeProvider)
    {
        //thanks to https://stackoverflow.com/questions/37287427/get-multiple-connection-strings-in-appsettings-json-without-ef
        _connectionDict = connectionsAccessor.Value;
        _shardingSettings = shardingSettingsAccessor.Value;
        _context = context;
        _options = options;
        DatabaseProviderMethods = databaseProviderMethods.ToDictionary(x => x.DatabaseProviderType.ToString());
        _localizeDefault = localizeProvider.DefaultLocalizer;

        //If no sharding settings file, then we provide one default sharding settings data
        //NOTE that the DatabaseInformation class has default values linked to a 
        _shardingSettings.ShardingDatabases ??= new List<DatabaseInformation>
        {
            DatabaseInformation.FormDefaultDatabaseInfo(options)
        };
    }



    /// <summary>
    /// This returns all the database names in the sharding settings file
    /// See <see cref="ShardingSettingsOption"/> for the format of that file
    /// NOTE: If the sharding settings file is missing, or there is no "ShardingData" section,
    /// then it will return one <see cref="ShardingSettingsOption"/> that uses the "DefaultConnection" connection string
    /// </summary>
    /// <returns>A list of <see cref="DatabaseInformation"/> from the sharding settings file</returns>
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
    /// This returns all the database info names in the sharding settings file, with a list of tenant name linked to each connection name
    /// NOTE: The DatabaseInfoName which matches the <see cref="AuthPermissionsOptions.ShardingDefaultDatabaseInfoName"/> is always
    /// returns a HasOwnDb value of false. This is because the default database has the AuthP data in it.
    /// </summary>
    /// <returns>List of all the database info names with the tenants using that database data name
    /// NOTE: The hasOwnDb is true for a database containing a single database, false for multiple tenant database and null if empty</returns>
    public async Task<List<(string databaseInfoName, bool? hasOwnDb, List<string> tenantNames)>> GetDatabaseInfoNamesWithTenantNamesAsync()
    {
        var nameAndConnectionName = await _context.Tenants
            .Select(x => new { ConnectionName = x.DatabaseInfoName, x })
            .ToListAsync();

        var grouped = nameAndConnectionName.GroupBy(x => x.ConnectionName)
            .ToDictionary(x => x.Key,
                y => y.Select(z => new { z.x.HasOwnDb, z.x.TenantFullName }));

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
        
        if (!DatabaseProviderMethods.TryGetValue(databaseData.DatabaseType,
                out IDatabaseSpecificMethods databaseSpecificMethods))
            throw new AuthPermissionsException($"The {databaseData.DatabaseType} database provider isn't supported");
        
        var status = databaseSpecificMethods.SetDatabaseInConnectionString(databaseData, connectionString);
        status.IfErrorsTurnToException();
        return status.Result;
    }

    /// <summary>
    /// This method allows you to check that the <see cref="DatabaseInformation"/> will create a
    /// a valid connection string. Useful when adding or editing the data in the sharding settings file.
    /// </summary>
    /// <param name="databaseInfo">The full definition of the <see cref="DatabaseInformation"/> for this database info</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public IStatusGeneric TestFormingConnectionString(DatabaseInformation databaseInfo)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);

        if (databaseInfo == null)
            throw new ArgumentNullException(nameof(databaseInfo));

        if (!_connectionDict.TryGetValue(databaseInfo.ConnectionName, out var connectionString))
            return status.AddErrorFormatted("NoConnectionString".ClassLocalizeKey(this, true),
                $"The {nameof(DatabaseInformation.ConnectionName)} '{databaseInfo.ConnectionName}' ",
                $"wasn't found in the connection strings.");

        if (!DatabaseProviderMethods.TryGetValue(databaseInfo.DatabaseType,
                out IDatabaseSpecificMethods databaseSpecificMethods))
            throw new AuthPermissionsException($"The {databaseInfo.DatabaseType} database provider isn't supported");
        try
        {
            databaseSpecificMethods.SetDatabaseInConnectionString(databaseInfo, connectionString);
        }
        catch
        {
            status.AddErrorFormatted("BadConnectionString".ClassLocalizeKey(this, true),
                $"There was an  error when trying to create a connection string. Typically this is because ",
                $"the connection string doesn't match the {nameof(DatabaseInformation.DatabaseType)}.");
        }

        return status;
    }
}