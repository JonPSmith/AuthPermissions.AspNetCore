// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Text.Json;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using StatusGeneric;

namespace AuthPermissions.AspNetCore.ShardingServices;

/// <summary>
/// This class contains CRUD methods to the sharding settings which contains a list of <see cref="DatabaseInformation"/>
/// The "Ver5" added the name makes sure users using this will get a compile error. See the UpdateToVersion5.md file
/// </summary>
public class AccessDatabaseInformationVer5 : IAccessDatabaseInformationVer5
{
    private readonly AuthPermissionsDbContext _authDbContext;
    private readonly IShardingConnections _connectionsService;
    private readonly IDefaultLocalizer _localizeDefault;
    private readonly AuthPermissionsOptions _options;

    private readonly string _settingsFilePath;

    /// <summary>
    /// Name of the sharding file
    /// </summary>
    public readonly string ShardingSettingFilename;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="env"></param>
    /// <param name="connectionsService"></param>
    /// <param name="authDbContext"></param>
    /// <param name="options"></param>
    /// <param name="localizeProvider"></param>
    public AccessDatabaseInformationVer5(IWebHostEnvironment env, IShardingConnections connectionsService, 
        AuthPermissionsDbContext authDbContext, AuthPermissionsOptions options, 
        IAuthPDefaultLocalizer localizeProvider)
    {
        ShardingSettingFilename = AuthPermissionsOptions.FormShardingSettingsFileName(options.SecondPartOfShardingFile);
        _settingsFilePath = Path.Combine(env.ContentRootPath, ShardingSettingFilename);
        _connectionsService = connectionsService;
        _authDbContext = authDbContext;
        _options = options;
        _localizeDefault = localizeProvider.DefaultLocalizer;
    }

    /// <summary>
    /// This will return a list of <see cref="DatabaseInformation"/> in the sharding settings file in the application
    /// </summary>
    /// <returns>If no file, then returns the default list</returns>
    public List<DatabaseInformation> ReadShardingSettingsFile()
    {
        if (!File.Exists(_settingsFilePath))
            return new List<DatabaseInformation>
                { DatabaseInformation.FormDefaultDatabaseInfo(_options, _authDbContext)};

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
    /// This adds a new <see cref="DatabaseInformation"/> to the list in the current sharding settings file.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param databaseInfoName="databaseInfo"></param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric AddDatabaseInfoToJsonFile(DatabaseInformation databaseInfo)
    {
        if (!_connectionsService.DatabaseProviderMethods.TryGetValue(databaseInfo.DatabaseType,
                out IDatabaseSpecificMethods databaseSpecificMethods))
            throw new AuthPermissionsException($"The {databaseInfo.DatabaseType} database provider isn't supported");

        return databaseSpecificMethods.ChangeDatabaseInformationWithinDistributedLock(
            _authDbContext.Database.GetConnectionString(), () =>
        {
            var fileContent = ReadShardingSettingsFile();
            fileContent.Add(databaseInfo);
            return CheckDatabasesInfoAndSaveIfValid(fileContent, databaseInfo);
        });
    }

    /// <summary>
    /// This updates a <see cref="DatabaseInformation"/> already in the sharding settings file.
    /// It uses the <see cref="DatabaseInformation.Name"/> in the provided in the <see cref="DatabaseInformation"/> parameter.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param databaseInfoName="databaseInfo"></param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric UpdateDatabaseInfoToJsonFile(DatabaseInformation databaseInfo)
    {
        if (!_connectionsService.DatabaseProviderMethods.TryGetValue(databaseInfo.DatabaseType,
                out IDatabaseSpecificMethods databaseSpecificMethods))
            throw new AuthPermissionsException($"The {databaseInfo.DatabaseType} database provider isn't supported");

        return databaseSpecificMethods.ChangeDatabaseInformationWithinDistributedLock(
            _authDbContext.Database.GetConnectionString(), () =>
        {
            var status = new StatusGenericLocalizer(_localizeDefault);
            var fileContent = ReadShardingSettingsFile();
            var foundIndex = fileContent.FindIndex(x => x.Name == databaseInfo.Name);
            if (foundIndex == -1)
                return status.AddErrorFormatted("MissingDatabaseInfo".ClassLocalizeKey(this, true),
                    $"Could not find a database info entry with the {nameof(DatabaseInformation.Name)} of '{databaseInfo.Name ?? "< null >"}'.");

            fileContent[foundIndex] = databaseInfo;
            return CheckDatabasesInfoAndSaveIfValid(fileContent, databaseInfo);
        });
    }

    /// <summary>
    /// This removes a <see cref="DatabaseInformation"/> with the same <see cref="DatabaseInformation.Name"/> as the databaseInfoName.
    /// If there are no errors it will update the sharding settings file in the application
    /// </summary>
    /// <param name="databaseInfoName">Looks for a <see cref="DatabaseInformation"/> with the <see cref="DatabaseInformation.Name"/> </param>
    /// <returns>status containing a success message, or errors</returns>
    public async Task<IStatusGeneric> RemoveDatabaseInfoToJsonFileAsync(string databaseInfoName)
    {
        var databaseType = _options.InternalData.AuthPDatabaseType.ToString();
        if (!_connectionsService.DatabaseProviderMethods.TryGetValue(databaseType,
                out IDatabaseSpecificMethods databaseSpecificMethods))
            throw new AuthPermissionsException($"The {databaseType} database provider isn't supported");

        return await databaseSpecificMethods.ChangeDatabaseInformationWithinDistributedLockAsync(
            _authDbContext.Database.GetConnectionString(), async () =>
        {
            var status = new StatusGenericLocalizer(_localizeDefault);
            var fileContent = ReadShardingSettingsFile();
            var foundIndex = fileContent.FindIndex(x => x.Name == databaseInfoName);
            if (foundIndex == -1)
                return status.AddErrorFormatted("MissingDatabaseInfo".ClassLocalizeKey(this, true),
                    $"Could not find a database info entry with the {nameof(DatabaseInformation.Name)} of '{databaseInfoName ?? "< null >"}'");

            var tenantsUsingThis = (await _connectionsService.GetDatabaseInfoNamesWithTenantNamesAsync())
                .SingleOrDefault(x => x.databaseInfoName == databaseInfoName).tenantNames;
            if (tenantsUsingThis.Count > 0)
                return status.AddErrorFormatted("NoDeleteAsUsed".ClassLocalizeKey(this, true),
                    $"You can't delete the database information with the {nameof(DatabaseInformation.Name)} of ",
                    $"{databaseInfoName} because {tenantsUsingThis.Count} tenant(s) uses this database.");

            fileContent.RemoveAt(foundIndex);
            return CheckDatabasesInfoAndSaveIfValid(fileContent, null);
        });
    }

    //-----------------------------------------------
    // private methods

    private IStatusGeneric CheckDatabasesInfoAndSaveIfValid(List<DatabaseInformation> databasesInfo, DatabaseInformation changedInfo)
    {
        var status = new StatusGenericLocalizer(_localizeDefault);
        status.SetMessageFormatted("SuccessUpdate".ClassLocalizeKey(this, true),
        $"Successfully updated the {ShardingSettingFilename} with your changes.");

        //Check Names: not null and unique 
        if (databasesInfo.Any(x => x.Name == null))
            return status.AddErrorString("DbNameMissing".ClassLocalizeKey(this, true),
                $"The {nameof(DatabaseInformation.Name)} is null, which isn't allowed.");
        var duplicates = databasesInfo.GroupBy(x => x.Name)
            .Where(g => g.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicates.Any())
            return status.AddErrorFormatted("DatabaseInfoDuplicate".ClassLocalizeKey(this, true),
                $"The {nameof(DatabaseInformation.Name)} of {string.Join(",", duplicates)} is already used.");

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