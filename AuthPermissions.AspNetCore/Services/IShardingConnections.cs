// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.SetupCode;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthPermissions.AspNetCore.Services;

/// <summary>
/// This is used when <see cref="TenantTypes.AddSharding"/> is turned on.
/// It uses the shardingsetting.json file which holds the information of the different databases the app can use. 
/// </summary>
public interface IShardingConnections
{
    /// <summary>
    /// This returns all the database names in the shardingsetting.json file
    /// See <see cref="ShardingSettingsOption"/> for the format of that file
    /// NOTE: If the shardingsetting.json file is missing, or there is no "ShardingData" section,
    /// then it will return one <see cref="ShardingSettingsOption"/> that uses the "DefaultConnection" connection string
    /// </summary>
    /// <returns>A list of <see cref="DatabaseInformation"/> from the shardingsetting.json file</returns>
    List<DatabaseInformation> GetAllPossibleShardingData();

    /// <summary>
    /// This provides the names of the connection string
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetConnectionStringNames();

    /// <summary>
    /// This returns all the database info names in the shardingsetting.json file, with a list of tenant name linked to each connection name
    /// </summary>
    /// <returns>List of all the database info names with the tenants using that database data name</returns>
    Task<List<(string databaseInfoName, List<string> tenantNames)>> GetDatabaseInfoNamesWithTenantNamesAsync();

    /// <summary>
    /// This will provide the connection string for the entry with the given sharding database info name
    /// </summary>
    /// <param name="databaseInfoName">The name of sharding database info we want to access</param>
    /// <returns>The connection string, or null if not found</returns>
    string FormConnectionString(string databaseInfoName);
}