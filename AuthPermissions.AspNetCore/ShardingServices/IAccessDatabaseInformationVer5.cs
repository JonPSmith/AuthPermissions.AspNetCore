// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This defines the CRUD methods to the sharding settings file which contains a list of <see cref="ShardingEntry"/>
/// The "Ver5" added the name makes sure users using this will get a compile error. See the UpdateToVersion5.md file 
/// </summary>
public interface IAccessDatabaseInformationVer5
{
    /// <summary>
    /// This will return a list of <see cref="ShardingEntry"/> in the sharding settings file in the application
    /// </summary>
    /// <returns>If data, then returns the default list. This handles the situation where the <see cref="ShardingEntry"/> isn't set up.</returns>
    List<ShardingEntry> ReadAllShardingInformation();

    /// <summary>
    /// This returns the <see cref="ShardingEntry"/> where its <see cref="ShardingEntry.Name"/> matches the databaseInfoName property.
    /// </summary>
    /// <param name="databaseInfoName">The Name of the <see cref="ShardingEntry"/> you are looking for</param>
    /// <returns>If no matching database information found, then it returns null</returns>
    ShardingEntry GetDatabaseInformationByName(string databaseInfoName);

    /// <summary>
    /// This adds a new <see cref="ShardingEntry"/> to the list in the current sharding settings file.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param name="databaseInfo">Adds a new <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> to the sharding data.</param>
    /// <returns>status containing a success message, or errors</returns>
    IStatusGeneric AddDatabaseInfoToShardingInformation(ShardingEntry databaseInfo);

    /// <summary>
    /// This updates a <see cref="ShardingEntry"/> already in the sharding settings file.
    /// It uses the <see cref="ShardingEntry.Name"/> in the provided in the <see cref="ShardingEntry"/> parameter.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param name="databaseInfo">Looks for a <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> and updates it.</param>
    /// <returns>status containing a success message, or errors</returns>
    IStatusGeneric UpdateDatabaseInfoToShardingInformation(ShardingEntry databaseInfo);

    /// <summary>
    /// This removes a <see cref="ShardingEntry"/> with the same <see cref="ShardingEntry.Name"/> as the databaseInfoName.
    /// If there are no errors it will update the sharding settings file in the application
    /// </summary>
    /// <param name="databaseInfoName">Looks for a <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> and removes it.</param>
    /// <returns>status containing a success message, or errors</returns>
    Task<IStatusGeneric> RemoveDatabaseInfoFromShardingInformationAsync(string databaseInfoName);
}