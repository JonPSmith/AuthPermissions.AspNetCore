// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.Services;
using StatusGeneric;

namespace AuthPermissions.SupportCode.ShardingServices;

/// <summary>
/// This defines the CRUD methods to the shardingsettings.json which contains a list of <see cref="DatabaseInformation"/> 
/// </summary>
public interface IAccessDatabaseInformation
{
    /// <summary>
    /// This will return a list of <see cref="DatabaseInformation"/> in the shardingsettings.json file in the application
    /// </summary>
    /// <returns>If no file, then returns an empty list</returns>
    List<DatabaseInformation> ReadShardingSettingsFile();

    /// <summary>
    /// This returns the <see cref="DatabaseInformation"/> where its <see cref="DatabaseInformation.Name"/> matches the databaseInfoName property.
    /// </summary>
    /// <param databaseInfoName="databaseInfoName"></param>
    /// <returns>If no matching database information found, then it returns null</returns>
    DatabaseInformation GetDatabaseInformationByName(string databaseInfoName);

    /// <summary>
    /// This adds a new <see cref="DatabaseInformation"/> to the list in the current shardingsettings.json file.
    /// If there are no errors it will update the shardingsettings.json file in the application.
    /// </summary>
    /// <param databaseInfoName="databaseInfo"></param>
    /// <returns>status containing a success message, or errors</returns>
    IStatusGeneric AddDatabaseInfoToJsonFile(DatabaseInformation databaseInfo);

    /// <summary>
    /// This updates a <see cref="DatabaseInformation"/> already in the shardingsettings.json file.
    /// It uses the <see cref="DatabaseInformation.Name"/> in the provided in the <see cref="DatabaseInformation"/> parameter.
    /// If there are no errors it will update the shardingsettings.json file in the application.
    /// </summary>
    /// <param databaseInfoName="databaseInfo"></param>
    /// <returns>status containing a success message, or errors</returns>
    IStatusGeneric UpdateDatabaseInfoToJsonFile(DatabaseInformation databaseInfo);

    /// <summary>
    /// This removes a <see cref="DatabaseInformation"/> with the same <see cref="DatabaseInformation.Name"/> as the databaseInfoName.
    /// If there are no errors it will update the shardingsettings.json file in the application
    /// </summary>
    /// <param databaseInfoName="databaseInfoName">Looks for a <see cref="DatabaseInformation"/> with the <see cref="DatabaseInformation.Name"/> </param>
    /// <returns>status containing a success message, or errors</returns>
    Task<IStatusGeneric> RemoveDatabaseInfoToJsonFileAsync(string databaseInfoName);
}