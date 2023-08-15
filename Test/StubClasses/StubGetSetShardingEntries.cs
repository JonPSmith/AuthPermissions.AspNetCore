// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using StatusGeneric;
using TestSupport.Helpers;

namespace Test.StubClasses;

public class StubGetSetShardingEntries : IGetSetShardingEntries
{
    private readonly object _caller;
    public string CalledMethodName { get; private set; }

    /// <summary>
    /// This contains the <see cref="ShardingEntry"/> data when add, update or delete are called 
    /// </summary>
    public ShardingEntry SharingEntryAddUpDel { get; private set; }

    /// <summary>
    /// This returns the supported database providers that can be used for multi tenant sharding.
    /// Only useful if you have multiple database providers for your tenant databases (rare).
    /// </summary>
    public string[] PossibleDatabaseProviders { get; }

    public StubGetSetShardingEntries(object caller)
    {
        PossibleDatabaseProviders = new string[]
        {
            "SqlServer",
            "PostgreSQL", 
            "SqliteInMemory"
        };

        _caller = caller;
    }



    /// <summary>
    /// This will return a list of <see cref="ShardingEntry"/> in the sharding settings file in the application
    /// </summary>
    /// <returns>If data, then returns the default list. This handles the situation where the <see cref="ShardingEntry"/> isn't set up.</returns>
    public List<ShardingEntry> GetAllShardingEntries()
    {
        return new List<ShardingEntry>
        {
            new ShardingEntry{Name = "Default Database", ConnectionName = "UnitTestConnection"},
            new ShardingEntry{Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "UnitTestConnection"},
            new ShardingEntry{Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = "Postgres"}
        };
    }

    /// <summary>
    /// This returns a <see cref="ShardingEntry"/> where the <see cref="ShardingEntry.Name"/> matches
    /// the <see cref="shardingEntryName"/> parameter. 
    /// </summary>
    /// <param name="shardingEntryName">The name of the <see cref="ShardingEntry"/></param>
    /// <returns>Returns the found <see cref="ShardingEntry"/>, or null if not found.</returns>
    public ShardingEntry GetSingleShardingEntry(string shardingEntryName)
    {
        CalledMethodName = nameof(GetSingleShardingEntry);
        return GetAllShardingEntries().SingleOrDefault(x => x.Name == shardingEntryName);
    }

    /// <summary>
    /// This adds a new <see cref="ShardingEntry"/> to the list in the current sharding settings file.
    /// If there are no errors it will update the sharding settings file in the application.
    /// </summary>
    /// <param name="shardingEntry">Adds a new <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> to the sharding data.</param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric AddNewShardingEntry(ShardingEntry shardingEntry)
    {
        CalledMethodName = nameof(AddNewShardingEntry);
        SharingEntryAddUpDel = shardingEntry;
        return new StatusGenericHandler();
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
        CalledMethodName = nameof(UpdateShardingEntry);
        SharingEntryAddUpDel = shardingEntry;
        return new StatusGenericHandler();
    }

    /// <summary>
    /// This removes a <see cref="ShardingEntry"/> with the same <see cref="ShardingEntry.Name"/> as the databaseInfoName.
    /// If there are no errors it will update the sharding settings file in the application.
    /// WARNING: This can remove a <see cref="ShardingEntry"/> wh
    /// </summary>
    /// <param name="shardingEntryName">Looks for a <see cref="ShardingEntry"/> with the <see cref="ShardingEntry.Name"/> and removes it.</param>
    /// <returns>status containing a success message, or errors</returns>
    public IStatusGeneric RemoveShardingEntry(string shardingEntryName)
    {
        CalledMethodName = nameof(RemoveShardingEntry);
        SharingEntryAddUpDel = new ShardingEntry { Name = shardingEntryName };
        return new StatusGenericHandler();
    }

    public List<string> GetConnectionStringNames()
    {
        return new List<string> { "UnitTestConnection", "PostgreSqlConnection" };
    }

    public IStatusGeneric TestFormingConnectionString(ShardingEntry databaseInfo)
    {
        var status = new StatusGenericHandler();
        return status;
    }

    public string FormConnectionString(string databaseInfoName)
    {
        return databaseInfoName switch
        {
            "Default Database" => _caller.GetUniqueDatabaseConnectionString("main"),
            "Other Database" => _caller.GetUniqueDatabaseConnectionString("other"),
            "PostgreSql1" => _caller.GetUniquePostgreSqlConnectionString(),
            _ => null
        };
    }

    public Task<List<(string shardingName, bool? hasOwnDb, List<string> tenantNames)>> GetShardingsWithTenantNamesAsync()
    {
        return Task.FromResult(new List<(string key, bool? hasOwnDb, List<string> tenantNames)>
        {
            ("Default Database", false, new List<string>{ "Tenant1","Tenant3"}),
            ("Other Database", true, new List<string>{ "Tenant2"}),
            ("PostgreSql1", null, new List<string>())
        });
    }
}