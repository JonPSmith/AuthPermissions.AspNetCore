// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Net.DistributedFileStoreCache;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This is an implementation of the <see cref="IGetSetShardingEntries"/> using the Net.DistributedFileStoreCache
/// library. This provides the reading and writing of of the ShardingEntries stored in the FileStoreCache.
/// </summary>
public class GetSetShardingEntriesFileStoreCache : IGetSetShardingEntries
{
    /// <summary>
    /// This is the prefix for creating the key to a sharding entry  
    /// </summary>
    public static string ShardingEntryPrefix = "ShardingEntry-";

    /// <summary>
    /// This contains the methods with are specific to a database provider
    /// </summary>
    private readonly IReadOnlyDictionary<string, IDatabaseSpecificMethods> _shardingDatabaseProviders;
    private readonly ConnectionStringsOption _connectionDict;
    private readonly ShardingEntryOptions _shardingEntryOptions;
    private readonly AuthPermissionsOptions _options;
    private readonly AuthPermissionsDbContext _authDbContext;
    private readonly IDistributedFileStoreCacheClass _fsCache;
    private readonly IDefaultLocalizer _localizeDefault;

    /// <summary>
    /// This returns the supported database providers that can be used for multi tenant sharding.
    /// Only useful if you have multiple database providers for your tenant databases (rare).
    /// </summary>
    public string[] PossibleDatabaseProviders { get; }

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="connectionsAccessor"></param>
    /// <param name="defaultInformationOptions"></param>
    /// <param name="options"></param>
    /// <param name="authDbContext"></param>
    /// <param name="fsCache"></param>
    /// <param name="databaseProviderMethods"></param>
    /// <param name="localizeProvider"></param>
    public GetSetShardingEntriesFileStoreCache(
        IOptionsSnapshot<ConnectionStringsOption> connectionsAccessor, ShardingEntryOptions defaultInformationOptions,
        AuthPermissionsOptions options, AuthPermissionsDbContext authDbContext, 
        IDistributedFileStoreCacheClass fsCache, IEnumerable<IDatabaseSpecificMethods> databaseProviderMethods,
        IAuthPDefaultLocalizer localizeProvider)
    {
        //thanks to https://stackoverflow.com/questions/37287427/get-multiple-connection-strings-in-appsettings-json-without-ef
        _connectionDict = connectionsAccessor?.Value ?? throw new ArgumentNullException(nameof(connectionsAccessor));

        _shardingEntryOptions = defaultInformationOptions ?? throw new ArgumentNullException(nameof(defaultInformationOptions));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _authDbContext = authDbContext ?? throw new ArgumentNullException(nameof(authDbContext));
        _fsCache = fsCache ?? throw new ArgumentNullException(nameof(fsCache));

        _shardingDatabaseProviders = databaseProviderMethods.ToDictionary(x => x.DatabaseProviderShortName);
        PossibleDatabaseProviders = _shardingDatabaseProviders.Keys.Distinct().OrderBy(x => x).ToArray();
        _localizeDefault = localizeProvider.DefaultLocalizer;

        if (!_shardingEntryOptions.HybridMode && _connectionDict.Count > 1)
            //This will remove the default connection string, if there are other connection strings
            //This is useful when you only have shard tenant (i.e. the tenant's HasOwnDb is true) 
            _connectionDict.Remove(_shardingEntryOptions.DefaultConnectionName);
    }

    /// <summary>
    /// This will return a list of <see cref="ShardingEntry"/> in the sharding settings file in the application
    /// </summary>
    /// <returns>If data, then returns the default list. This handles the situation where the <see cref="ShardingEntry"/> isn't set up.</returns>
    public List<ShardingEntry> GetAllShardingEntries()
    {
        var results = _fsCache.GetAllKeyValues()
            .Where(kv => kv.Key.StartsWith(ShardingEntryPrefix)).ToList()
            .Select(s => _fsCache.GetClassFromString<ShardingEntry>(s.Value)).ToList();

        if (results.Any() || !_shardingEntryOptions.HybridMode) 
            return results;

        //If no entries and AddIfEntry is true, then its most likely an new deployment and the cache isn't setup
        //Se we add the default sharding entry to the cache and return the default Entry
        var defaultEntry = _shardingEntryOptions
            .ProvideDefaultShardingEntry(_options, _authDbContext);
        results.Add(defaultEntry);

        return results;
    }

    /// <summary>
    /// This returns a <see cref="ShardingEntry"/> where the <see cref="ShardingEntry.Name"/> matches
    /// the <see cref="shardingEntryName"/> parameter. 
    /// </summary>
    /// <param name="shardingEntryName"></param>
    /// <returns>Returns the found <see cref="ShardingEntry"/>, or null if not found.</returns>
    public ShardingEntry GetSingleShardingEntry(string shardingEntryName)
    {
        var entry = _fsCache.GetClass<ShardingEntry>(FormShardingEntryKey(shardingEntryName));

        //If no entries it might because this is the first deployment and the cache isn't setup
        return entry == null && _shardingEntryOptions.HybridMode 
            && shardingEntryName == _options.DefaultShardingEntryName
            ? _shardingEntryOptions.ProvideDefaultShardingEntry(_options, _authDbContext)
            : entry;
    }

    /// <summary>
    /// This adds a new <see cref="ShardingEntry"/> to the list in the current sharding settings file.
    /// If there are no errors it will update the sharding settings in the FileStore cache.
    /// </summary>
    /// <param name="shardingEntry">Adds a new <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> to the sharding data.</param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric AddNewShardingEntry(ShardingEntry shardingEntry)
    {
        if (_shardingEntryOptions.HybridMode &&
            !_fsCache
                .GetAllKeyValues().Any(kv => kv.Key.StartsWith(ShardingEntryPrefix)))
        {
            //If no entries and AddIfEntry is true, then its most likely an new deployment and the cache isn't setup
            //Se we add the default sharding entry to the cache and return the default Entry
            var defaultEntry = _shardingEntryOptions
                .ProvideDefaultShardingEntry(_options, _authDbContext);
            _fsCache.SetClass(FormShardingEntryKey(defaultEntry.Name), defaultEntry);
        }

        var status = CheckShardingEntryChangeIsValid(ShardingChanges.Added, shardingEntry);
        if (status.IsValid)
            _fsCache.SetClass(FormShardingEntryKey(shardingEntry.Name), shardingEntry);

        return status;
    }

    /// <summary>
    /// This updates a <see cref="ShardingEntry"/> already in the sharding settings file.
    /// It uses the <see cref="ShardingEntry.Name"/> in the provided in the <see cref="ShardingEntry"/> parameter.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param name="shardingEntry">Looks for a <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> and updates it.</param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric UpdateShardingEntry(ShardingEntry shardingEntry)
    {
        var status = CheckShardingEntryChangeIsValid(ShardingChanges.Updated, shardingEntry);
        if (status.IsValid)
            _fsCache.SetClass(FormShardingEntryKey(shardingEntry.Name), shardingEntry);

        return status;
    }

    /// <summary>
    /// This removes a <see cref="ShardingEntry"/> with the same <see cref="ShardingEntry.Name"/> as the databaseInfoName.
    /// If there are no errors it will update the sharding settings in the FileStore.
    /// </summary>
    /// <param name="shardingEntryName">Looks for a <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> and removes it.</param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric RemoveShardingEntry(string shardingEntryName)
    {
        var fullShardingInfo = new ShardingEntry { Name = shardingEntryName };
        var status = CheckShardingEntryChangeIsValid(ShardingChanges.Deleted, fullShardingInfo);
        if (status.IsValid)
            _fsCache.Remove(FormShardingEntryKey(fullShardingInfo.Name));

        return status;
    }

    /// <summary>
    /// This provides the name of the connection strings. This allows you have connection strings
    /// linked to different servers, e.g. WestServer, CenterServer and EastServer (see Example6)
    /// </summary>
    /// <returns></returns>
    public List<string> GetConnectionStringNames()
    {
        return _connectionDict.Keys.ToList();
    }

    /// <summary>
    /// This returns all the sharding entries names in the sharding settings file, with a list of tenant name linked to each connection name
    /// NOTE: The shardingName which matches the <see cref="AuthPermissionsOptions.DefaultShardingEntryName"/> is always
    /// returns a HasOwnDb value of false. This is because the default database has the AuthP data in it.
    /// </summary>
    /// <returns>List of all the sharding entries names with the tenants using that database data name
    /// NOTE: The hasOwnDb is true for a database containing a single database, false for multiple tenant database and null if empty</returns>
    public async Task<List<(string shardingName, bool? hasOwnDb, List<string> tenantNames)>> GetShardingsWithTenantNamesAsync()
    {
        var nameAndConnectionName = await _authDbContext.Tenants
            .Select(x => new { ConnectionName = x.DatabaseInfoName, x })
            .ToListAsync();

        var grouped = nameAndConnectionName.GroupBy(x => x.ConnectionName)
            .ToDictionary(x => x.Key,
                y => y.Select(z => new { z.x.HasOwnDb, z.x.TenantFullName }));

        var result = new List<(string databaseInfoName, bool? hasOwnDb, List<string>)>();
        //Add sharding database names that have no tenants in them so that you can see all the connection string  names
        foreach (var databaseInfoName in GetAllShardingEntries().Select(x => x.Name))
        {
            result.Add(grouped.ContainsKey(databaseInfoName)
                ? (databaseInfoName,
                    databaseInfoName == _options.DefaultShardingEntryName
                        ? false //The default DatabaseInfoName contains the AuthP information, so its a shared database
                        : grouped[databaseInfoName].FirstOrDefault()?.HasOwnDb,
                    grouped[databaseInfoName].Select(x => x.TenantFullName).ToList())
                : (databaseInfoName,
                    databaseInfoName == _options.DefaultShardingEntryName ? false : null,
                    new List<string>()));
        }

        return result;
    }

    /// <summary>
    /// This will provide the connection string for the entry with the given sharding entry name
    /// </summary>
    /// <param name="shardingEntryName">The name of sharding entry we want to access</param>
    /// <returns>The connection string, or throw exception</returns>
    public string FormConnectionString(string shardingEntryName)
    {
        if (shardingEntryName == null)
            throw new AuthPermissionsException("The name of the database date can't be null");

        var databaseData = GetSingleShardingEntry(shardingEntryName);
        if (databaseData == null)
            throw new AuthPermissionsException(
                $"The database information with the name of '{shardingEntryName}' wasn't founds.");

        if (!_connectionDict.TryGetValue(databaseData.ConnectionName, out var connectionString))
            throw new AuthPermissionsException(
                $"Could not find the connection name '{databaseData.ConnectionName}' that the sharding database data '{shardingEntryName}' requires.");

        if (!_shardingDatabaseProviders.TryGetValue(databaseData.DatabaseType,
                out IDatabaseSpecificMethods databaseSpecificMethods))
            throw new AuthPermissionsException($"The {databaseData.DatabaseType} database provider isn't supported");

        return databaseSpecificMethods.FormShardingConnectionString(databaseData, connectionString);
    }

    //------------------------------------------------------
    //private methods

    private string FormShardingEntryKey(string shardingEntryName)
    {
        return ShardingEntryPrefix + shardingEntryName;
    }

    /// <summary>
    /// This method allows you to check that the <see cref="ShardingEntry"/> will create a
    /// a valid connection string. Useful when adding or editing the data in the ShardingEntry data.
    /// </summary>
    /// <param name="databaseInfo">The full definition of the <see cref="ShardingEntry"/> for this sharding entry</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    private IStatusGeneric TestFormingConnectionString(ShardingEntry databaseInfo)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);

        if (databaseInfo == null)
            throw new ArgumentNullException(nameof(databaseInfo));

        if (!_connectionDict.TryGetValue(databaseInfo.ConnectionName, out var connectionString))
            return status.AddErrorFormatted("NoConnectionString".ClassLocalizeKey(this, true),
                $"The {nameof(ShardingEntry.ConnectionName)} '{databaseInfo.ConnectionName}' ",
                $"wasn't found in the connection strings.");

        if (!_shardingDatabaseProviders.TryGetValue(databaseInfo.DatabaseType,
                out IDatabaseSpecificMethods databaseSpecificMethods))
            throw new AuthPermissionsException($"The {databaseInfo.DatabaseType} database provider isn't supported. " +
                $"You need to register a IDatabaseSpecificMethods method for that database type, e.g. SqlServerDatabaseSpecificMethods.");
        try
        {
            databaseSpecificMethods.FormShardingConnectionString(databaseInfo, connectionString);
        }
        catch
        {
            status.AddErrorFormatted("BadConnectionString".ClassLocalizeKey(this, true),
                $"There was an  error when trying to create a connection string. Typically this is because ",
                $"the connection string doesn't match the {nameof(ShardingEntry.DatabaseType)}.");
        }

        return status;
    }

    private enum ShardingChanges {Added, Updated, Deleted}

    private IStatusGeneric CheckShardingEntryChangeIsValid(ShardingChanges typeOfChange, ShardingEntry changedInfo)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);
        status.SetMessageFormatted("SuccessUpdate".ClassLocalizeKey(this, true),
            $"Successfully {typeOfChange} the {changedInfo.Name} sharding entry.");

        //Check Names: not null or empty
        if (changedInfo.Name.IsNullOrEmpty())
            return status.AddErrorString("NameNullOrEmpty".ClassLocalizeKey(this, true),
                $"The {nameof(ShardingEntry.Name)} is null or empty, which isn't allowed.");

        if (changedInfo.Name == _options.DefaultShardingEntryName 
            && _shardingEntryOptions.HybridMode)
            return status.AddErrorString("Name".ClassLocalizeKey(this, true),
                $"You can't add, update or delete the default sharding entry called '{_options.DefaultShardingEntryName}'.");

        //Now we check it isn't a duplicate (add) or is there (update and delete)
        var currentEntry = _fsCache.Get(FormShardingEntryKey(changedInfo.Name));
        switch (typeOfChange)
        {
            case ShardingChanges.Added:
                if (currentEntry != null)
                    return status.AddErrorString("DuplicateShardingEntry".ClassLocalizeKey(this, true),
                        $"There is already a sharding entry with the '{changedInfo.Name}'.");
                break;
            case ShardingChanges.Updated:
                if (currentEntry == null)
                    return status.AddErrorString("MissingShardingEntry".ClassLocalizeKey(this, true),
                        $"There isn't sharding entry with the '{changedInfo.Name}' to {typeOfChange}.");
                break;
            case ShardingChanges.Deleted:
                if (currentEntry == null)
                    return status.AddErrorString("MissingShardingEntry".ClassLocalizeKey(this, true),
                        $"There isn't sharding entry with the '{changedInfo.Name}' to {typeOfChange}.");
                var numUsersUsingThisSharding = _authDbContext.AuthUsers
                    .Count(x => x.UserTenant != null && x.UserTenant.DatabaseInfoName == changedInfo.Name);
                if (numUsersUsingThisSharding > 0)
                    return status.AddErrorString("UsedShardingEntry".ClassLocalizeKey(this, true),
                        $"You need to disconnect the {numUsersUsingThisSharding} users from the '{changedInfo.Name}' sharding entry before you can delete it.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(typeOfChange), typeOfChange, null);
        }

        //Try creating a valid connection string for new or updated entries
        if (typeOfChange != ShardingChanges.Deleted) //if deleted we don't test it
            status.CombineStatuses(TestFormingConnectionString(changedInfo));

        return status;
    }
}