// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This interface defines the methods for getting and setting the list of <see cref="ShardingEntry"/>
/// used to define the database that each tenant should access.
/// </summary>
public interface IGetSetShardingEntries
{

    /// <summary>
    /// This returns the supported database providers that can be used for multi tenant sharding.
    /// Only useful if you have multiple database providers for your tenant databases (rare).
    /// </summary>
    public string[] PossibleDatabaseProviders { get; }

    /// <summary>
    /// This will return a list of <see cref="ShardingEntry"/> in the sharding settings file in the application
    /// </summary>
    /// <returns>If data, then returns the default list. This handles the situation where the <see cref="ShardingEntry"/> isn't set up.</returns>
    List<ShardingEntry> GetAllShardingEntries();

    /// <summary>
    /// This returns a <see cref="ShardingEntry"/> where the <see cref="ShardingEntry.Name"/> matches
    /// the <see cref="shardingEntryName"/> parameter. 
    /// </summary>
    /// <param name="shardingEntryName">The name of the <see cref="ShardingEntry"/></param>
    /// <returns>Returns the found <see cref="ShardingEntry"/>, or null if not found.</returns>
    ShardingEntry GetSingleShardingEntry(string shardingEntryName);

    /// <summary>
    /// This adds a new <see cref="ShardingEntry"/> to the list in the current sharding settings file.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param name="shardingEntry">Adds a new <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> to the sharding data.</param>
    /// <returns>status containing a success message, or errors</returns>
    IStatusGeneric AddNewShardingEntry(ShardingEntry shardingEntry);

    /// <summary>
    /// This updates a <see cref="ShardingEntry"/> already in the sharding settings file.
    /// It uses the <see cref="ShardingEntry.Name"/> in the provided in the <see cref="ShardingEntry"/> parameter.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param name="shardingEntry">Looks for a <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> and updates it.</param>
    /// <returns>status containing a success message, or errors</returns>
    IStatusGeneric UpdateShardingEntry(ShardingEntry shardingEntry);

    /// <summary>
    /// This removes a <see cref="ShardingEntry"/> with the same <see cref="ShardingEntry.Name"/> as the databaseInfoName.
    /// If there are no errors it will update the sharding settings data in the application.
    /// </summary>
    /// <param name="shardingEntryName">Looks for a <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> and removes it.</param>
    /// <returns>status containing a success message, or errors</returns>
    IStatusGeneric RemoveShardingEntry(string shardingEntryName);

    /// <summary>
    /// This provides the name of the connection strings. This allows you have connection strings
    /// linked to different servers, e.g. WestServer, CenterServer and EastServer (see Example6)
    /// </summary>
    /// <returns></returns>
    List<string> GetConnectionStringNames();

    /// <summary>
    /// This returns all the database info names in the ShardingEntry data, with a list of tenant name linked to each connection name
    /// </summary>
    /// <returns>List of all the database info names with the tenants (and whether its sharding) within that database data name
    /// NOTE: The hasOwnDb is true for a database containing a single database, false for multiple tenant database and null if empty</returns>
    Task<List<(string shardingName, bool? hasOwnDb, List<string> tenantNames)>> GetShardingsWithTenantNamesAsync();

    /// <summary>
    /// This will provide the connection string for the entry with the given database info name
    /// </summary>
    /// <param name="shardingEntryName">The name of sharding database info we want to access</param>
    /// <returns>The connection string, or throw exception</returns>
    public string FormConnectionString(string shardingEntryName);
}