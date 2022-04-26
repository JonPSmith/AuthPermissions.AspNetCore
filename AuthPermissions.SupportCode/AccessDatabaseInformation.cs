// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Json;
using AuthPermissions.AspNetCore.Services;
using Microsoft.AspNetCore.Hosting;
using StatusGeneric;

namespace AuthPermissions.SupportCode;

/// <summary>
/// This class contains CRUD methods to the shardingsettings.json which contains a list of <see cref="DatabaseInformation"/> 
/// </summary>
public class AccessDatabaseInformation : IAccessDatabaseInformation
{
    /// <summary>
    /// Name of the file
    /// </summary>
    public const string ShardingSettingFilename = "shardingsettings.json";

    private readonly string _settingsFilePath;
    private readonly IShardingConnections _connectionsService;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="env"></param>
    /// <param name="connectionsService"></param>
    public AccessDatabaseInformation(IWebHostEnvironment env, IShardingConnections connectionsService)
    {
        _settingsFilePath = Path.Combine(env.ContentRootPath, ShardingSettingFilename);
        _connectionsService = connectionsService;
    }

    /// <summary>
    /// This will return a list of <see cref="DatabaseInformation"/> in the shardingsettings.json file in the application
    /// </summary>
    /// <returns>If no file, then returns an empty list</returns>
    public List<DatabaseInformation> ReadShardingSettingsFile()
    {
        if (!File.Exists(_settingsFilePath))
            return new List<DatabaseInformation>();

        var fileContext = File.ReadAllText(_settingsFilePath);
        var content = JsonSerializer.Deserialize<ShardingSettingsOption>(fileContext,
            new JsonSerializerOptions{ ReadCommentHandling = JsonCommentHandling.Skip});

        return content?.ShardingDatabases;
    }

    /// <summary>
    /// This returns the <see cref="DatabaseInformation"/> where its <see cref="DatabaseInformation.Name"/> matches the databaseInfoName property.
    /// </summary>
    /// <param databaseInfoName="databaseInfoName"></param>
    /// <returns>If no matching database information found, then it returns null</returns>
    public DatabaseInformation GetDatabaseInformationByName(string databaseInfoName)
    {
        return ReadShardingSettingsFile().SingleOrDefault(x => x.Name == databaseInfoName);
    }

    /// <summary>
    /// This adds a new <see cref="DatabaseInformation"/> to the list in the current shardingsettings.json file.
    /// If there are no errors it will update the shardingsettings.json file in the application.
    /// </summary>
    /// <param databaseInfoName="databaseInfo"></param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric AddDatabaseInfoToJsonFile(DatabaseInformation databaseInfo)
    {
        var fileContent = ReadShardingSettingsFile() ?? new List<DatabaseInformation>();
        fileContent.Add(databaseInfo);
        return CheckDatabasesInfoAndSaveIfValid(fileContent, databaseInfo);
    }

    /// <summary>
    /// This updates a <see cref="DatabaseInformation"/> already in the shardingsettings.json file.
    /// It uses the <see cref="DatabaseInformation.Name"/> in the provided in the <see cref="DatabaseInformation"/> parameter.
    /// If there are no errors it will update the shardingsettings.json file in the application.
    /// </summary>
    /// <param databaseInfoName="databaseInfo"></param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric UpdateDatabaseInfoToJsonFile(DatabaseInformation databaseInfo)
    {
        var status = new StatusGenericHandler();
        var fileContent = ReadShardingSettingsFile() ?? new List<DatabaseInformation>();
        var foundIndex = fileContent.FindIndex(x => x.Name == databaseInfo.Name);
        if (foundIndex == -1)
            return status.AddError("Could not find a database info entry with the " +
                                   $"{nameof(DatabaseInformation.Name)} of '{databaseInfo.Name ?? "< null >"}'");

        fileContent[foundIndex] = databaseInfo;
        return CheckDatabasesInfoAndSaveIfValid(fileContent, databaseInfo);
    }

    /// <summary>
    /// This removes a <see cref="DatabaseInformation"/> with the same <see cref="DatabaseInformation.Name"/> as the databaseInfoName.
    /// If there are no errors it will update the shardingsettings.json file in the application
    /// </summary>
    /// <param databaseInfoName="databaseInfoName">Looks for a <see cref="DatabaseInformation"/> with the <see cref="DatabaseInformation.Name"/> </param>
    /// <returns>status containing a success message, or errors</returns>
    public async Task<IStatusGeneric> RemoveDatabaseInfoToJsonFileAsync(string databaseInfoName)
    {
        var status = new StatusGenericHandler();
        var fileContent = ReadShardingSettingsFile() ?? new List<DatabaseInformation>();
        var foundIndex = fileContent.FindIndex(x => x.Name == databaseInfoName);
        if (foundIndex == -1)
            return status.AddError("Could not find a database info entry with the " +
                                   $"{nameof(DatabaseInformation.Name)} of '{databaseInfoName ?? "< null >"}'");

        var tenantsUsingThis = (await _connectionsService.GetDatabaseInfoNamesWithTenantNamesAsync())
            .SingleOrDefault(x => x.databaseInfoName == databaseInfoName).tenantNames;
        if (tenantsUsingThis.Count > 0)
            return status.AddError("You can't delete the database information with the " +
                                   $"{nameof(DatabaseInformation.Name)} of {databaseInfoName} because {tenantsUsingThis.Count} tenant(s) uses this database.");

        fileContent.RemoveAt(foundIndex);
        return CheckDatabasesInfoAndSaveIfValid(fileContent, null);
    }

    //-----------------------------------------------
    // private methods

    private IStatusGeneric CheckDatabasesInfoAndSaveIfValid(List<DatabaseInformation> databasesInfo, DatabaseInformation changedInfo)
    {
        var status = new StatusGenericHandler
            { Message = $"Successfully updated the {ShardingSettingFilename} with your changes." };

        //Check Names: not null and unique 
        if (databasesInfo.Any(x => x.Name == null))
            return status.AddError($"The {nameof(DatabaseInformation.Name)} is null, which isn't allowed.");
        var duplicates = databasesInfo.GroupBy(x => x.Name)
            .Where(g => g.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicates.Any())
            return status.AddError($"The {nameof(DatabaseInformation.Name)} of {String.Join(",", duplicates)} is already used.");

        //Try creating a valid connection string
        if (changedInfo != null) //if deleted we don't test it
            status.CombineStatuses(_connectionsService.TestFormingConnectionString(changedInfo));

        if (status.HasErrors)
            return status;

        var jsonString = JsonSerializer.Serialize(new ShardingSettingsOption{ ShardingDatabases = databasesInfo }, 
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, jsonString);

        return status;
    }
}