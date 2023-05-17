// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This is used when <see cref="TenantTypes.AddSharding"/> is turned on.
/// It uses the sharding settings file which holds the information of the different databases the app can use. 
/// </summary>
public interface IShardingConnections
{
    /// <summary>
    /// This contains the methods with are specific to a database provider
    /// </summary>
    public IReadOnlyDictionary<AuthPDatabaseTypes, IDatabaseSpecificMethods> DatabaseProviderMethods { get; }

    /// <summary>
    /// This returns the supported database provider that can be used for multi tenant sharding
    /// </summary>
    public IReadOnlyDictionary<string, IDatabaseSpecificMethods> ShardingDatabaseProviders { get; }

    /// <summary>
    /// This returns all the database names in the sharding settings file
    /// See <see cref="ShardingSettingsOption"/> for the format of that file
    /// NOTE: If the sharding settings file is missing, or there is no "ShardingData" section,
    /// then it will return one <see cref="ShardingSettingsOption"/> that uses the "DefaultConnection" connection string
    /// </summary>
    /// <returns>A list of <see cref="DatabaseInformation"/> from the sharding settings file</returns>
    List<DatabaseInformation> GetAllPossibleShardingData();

    /// <summary>
    /// This provides the names of the connection string
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetConnectionStringNames();

    /// <summary>
    /// This returns all the database info names in the sharding settings file, with a list of tenant name linked to each connection name
    /// </summary>
    /// <returns>List of all the database info names with the tenants (and whether its sharding) within that database data name
    /// NOTE: The hasOwnDb is true for a database containing a single database, false for multiple tenant database and null if empty</returns>
    Task<List<(string databaseInfoName, bool? hasOwnDb, List<string> tenantNames)>> GetDatabaseInfoNamesWithTenantNamesAsync();

    /// <summary>
    /// This method allows you to check that the <see cref="DatabaseInformation"/> will create a
    /// a valid connection string. Useful when adding or editing the data in the shardingsettings file.
    /// </summary>
    /// <param name="databaseInfo">The full definition of the <see cref="DatabaseInformation"/> for this database info</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    IStatusGeneric TestFormingConnectionString(DatabaseInformation databaseInfo);

    /// <summary>
    /// This will provide the connection string for the entry with the given sharding database info name
    /// </summary>
    /// <param name="databaseInfoName">The name of sharding database info we want to access</param>
    /// <returns>The connection string, or null if not found</returns>
    string FormConnectionString(string databaseInfoName);
}