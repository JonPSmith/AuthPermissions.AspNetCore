// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.Json;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Medallion.Threading.Postgres;
using Medallion.Threading.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using StatusGeneric;
using File = System.IO.File;

namespace AuthPermissions.SupportCode.ShardingServices;

/// <summary>
/// This class contains CRUD methods to the sharding settings which contains a list of <see cref="DatabaseInformation"/> 
/// </summary>
public class AccessDatabaseInformation : IAccessDatabaseInformation
{
    /// <summary>
    /// Name of the sharding file
    /// </summary>
    public readonly string ShardingSettingFilename;

    private readonly string _settingsFilePath;
    private readonly IShardingConnections _connectionsService;
    private readonly AuthPermissionsDbContext _authDbContext;
    private readonly AuthPermissionsOptions _options;

    /// <summary>
    /// Ctor
    /// </summary>
    /// <param name="env"></param>
    /// <param name="connectionsService"></param>
    /// <param name="authDbContext"></param>
    /// <param name="options"></param>
    public AccessDatabaseInformation(IWebHostEnvironment env, IShardingConnections connectionsService, 
        AuthPermissionsDbContext authDbContext, AuthPermissionsOptions options)
    {
        ShardingSettingFilename = AuthPermissionsOptions.FormShardingSettingsFileName(options.SecondPartOfShardingFile);
        _settingsFilePath = Path.Combine(env.ContentRootPath, ShardingSettingFilename);
        _connectionsService = connectionsService;
        _authDbContext = authDbContext;
        _options = options;
    }

    /// <summary>
    /// This will return a list of <see cref="DatabaseInformation"/> in the sharding settings file in the application
    /// </summary>
    /// <returns>If no file, then returns an empty list</returns>
    public List<DatabaseInformation> ReadShardingSettingsFile()
    {
        if (!File.Exists(_settingsFilePath))
            return new List<DatabaseInformation>
                { new DatabaseInformation { Name = _options.ShardingDefaultDatabaseInfoName } };

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
        return ApplyChangeWithinDistributedLock(() =>
        {
            var fileContent = ReadShardingSettingsFile() ?? new List<DatabaseInformation>();
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
        return ApplyChangeWithinDistributedLock(() =>
        {
            var status = new StatusGenericHandler();
            var fileContent = ReadShardingSettingsFile() ?? new List<DatabaseInformation>();
            var foundIndex = fileContent.FindIndex(x => x.Name == databaseInfo.Name);
            if (foundIndex == -1)
                return status.AddError("Could not find a database info entry with the " +
                                       $"{nameof(DatabaseInformation.Name)} of '{databaseInfo.Name ?? "< null >"}'");

            fileContent[foundIndex] = databaseInfo;
            return CheckDatabasesInfoAndSaveIfValid(fileContent, databaseInfo);
        });
    }

    /// <summary>
    /// This removes a <see cref="DatabaseInformation"/> with the same <see cref="DatabaseInformation.Name"/> as the databaseInfoName.
    /// If there are no errors it will update the sharding settings file in the application
    /// </summary>
    /// <param databaseInfoName="databaseInfoName">Looks for a <see cref="DatabaseInformation"/> with the <see cref="DatabaseInformation.Name"/> </param>
    /// <returns>status containing a success message, or errors</returns>
    public async Task<IStatusGeneric> RemoveDatabaseInfoToJsonFileAsync(string databaseInfoName)
    {
        return await ApplyChangeWithinDistributedLockAsync(async () =>
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
        });
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

    private IStatusGeneric ApplyChangeWithinDistributedLock(Func<IStatusGeneric> runInLock)
    {
        if (_authDbContext.Database.IsSqlServer())
        {
            var myDistributedLock = new SqlDistributedLock("Sharding!", _authDbContext.Database.GetConnectionString()!);
            using (myDistributedLock.Acquire())
            {
                return runInLock();
            }
        }
        
        if (_authDbContext.Database.IsNpgsql())
        {
            //See this as to why the name is 9 digits long https://github.com/madelson/DistributedLock/blob/master/docs/DistributedLock.Postgres.md#implementation-notes
            var myDistributedLock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("Sharding!", allowHashing: true), _authDbContext.Database.GetConnectionString()!); // e. g. if we are using SQL Server
            using (myDistributedLock.Acquire())
            {
                return runInLock();
            }
        }

        //its using some form of in-memory database so don't bother with a lock 
        return runInLock();
    }

    private async Task<IStatusGeneric> ApplyChangeWithinDistributedLockAsync(Func<Task<IStatusGeneric>> runInLockAsync)
    {
        if (_authDbContext.Database.IsSqlServer())
        {
            var myDistributedLock = new SqlDistributedLock("Sharding!", _authDbContext.Database.GetConnectionString()!);
            using (await myDistributedLock.AcquireAsync())
            {
                return await runInLockAsync();
            }
        }

        if (_authDbContext.Database.IsNpgsql())
        {
            //See this as to why the name is 9 digits long https://github.com/madelson/DistributedLock/blob/master/docs/DistributedLock.Postgres.md#implementation-notes
            var myDistributedLock = new PostgresDistributedLock(new PostgresAdvisoryLockKey("Sharding!", allowHashing: true), _authDbContext.Database.GetConnectionString()!); // e. g. if we are using SQL Server
            using (await myDistributedLock.AcquireAsync())
            {
                return await runInLockAsync();
            }
        }

        //its using some form of in-memory database so don't bother with a lock 
        return await runInLockAsync();
    }
}