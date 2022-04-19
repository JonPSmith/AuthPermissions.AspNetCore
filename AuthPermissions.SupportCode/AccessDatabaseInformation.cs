// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Json;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Microsoft.AspNetCore.Hosting;
using StatusGeneric;

namespace AuthPermissions.SupportCode;

public class AccessDatabaseInformation
{
    public const string ShardingSettingFilename = "shardingsettings.json";

    private readonly string _settingsFilePath;
    private readonly IShardingConnections _connectionsService;

    public AccessDatabaseInformation(IWebHostEnvironment env, IShardingConnections connectionsService)
    {
        _settingsFilePath = Path.Combine(env.ContentRootPath, ShardingSettingFilename);
        _connectionsService = connectionsService;

    }

    public List<DatabaseInformation> ReadShardingSettingsFile()
    {

        if (!File.Exists(_settingsFilePath))
            return null;

        var fileContext = File.ReadAllText(_settingsFilePath);
        var content = JsonSerializer.Deserialize<ShardingSettingsOption>(fileContext,
            new JsonSerializerOptions{ ReadCommentHandling = JsonCommentHandling.Skip});

        return content?.ShardingDatabases;
    }

    public IStatusGeneric AddDatabaseInfoToJsonFile(DatabaseInformation databaseInfo)
    {
        var fileContent = ReadShardingSettingsFile() ?? new List<DatabaseInformation>();
        fileContent.Add(databaseInfo);
        return CheckDatabasesInfoAndSaveIfValid(fileContent, databaseInfo);
    }

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

    public async Task<IStatusGeneric> DeleteDatabaseInfoToJsonFileAsync(string databaseInfoName)
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

        //Check the ConnectionName
        

        //Try creating a valid connection string
        if (changedInfo != null) //if deleted we don't test it
            try
            {
                if (!_connectionsService.GetConnectionStringNames().Contains(changedInfo.ConnectionName))
                    return status.AddError(
                        $"The {nameof(DatabaseInformation.ConnectionName)} '{changedInfo.ConnectionName}' " +
                        "wasn't found in the connection strings.");

                var connectionString = _connectionsService.FormConnectionString(changedInfo.Name);
            }
            catch (AuthPermissionsException e)
            {
                status.AddError(e.Message);
            }
            catch (Exception)
            {
                status.AddError(
                    "There was a system error when trying to create a connection string on the data you provides.");
            }

        if (status.HasErrors)
            return status;

        var jsonString = JsonSerializer.Serialize(new ShardingSettingsOption{ ShardingDatabases = databasesInfo }, 
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsFilePath, jsonString);

        return status;
    }
}