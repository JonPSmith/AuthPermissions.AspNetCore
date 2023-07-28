// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.AspNetCore.ShardingServices.DatabaseSpecificMethods;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.SetupCode;
using StatusGeneric;
using TestSupport.Helpers;

namespace Test.StubClasses;

public class StubConnectionsService : IShardingConnections
{
    private readonly object _caller;

    /// <summary>
    /// This contains the methods with are specific to a database provider
    /// </summary>
    public IReadOnlyDictionary<AuthPDatabaseTypes, IDatabaseSpecificMethods> DatabaseProviderMethods { get; }

    /// <summary>
    /// This returns the supported database provider that can be used for multi tenant sharding
    /// </summary>
    public IReadOnlyDictionary<string, IDatabaseSpecificMethods> ShardingDatabaseProviders { get; }

    public StubConnectionsService(object caller)
    {
        DatabaseProviderMethods = new Dictionary<AuthPDatabaseTypes, IDatabaseSpecificMethods>
        {
            { AuthPDatabaseTypes.SqlServer, new SqlServerDatabaseSpecificMethods() },
            { AuthPDatabaseTypes.PostgreSQL, new PostgresDatabaseSpecificMethods() },
            { AuthPDatabaseTypes.SqliteInMemory, new StubSqliteDatabaseSpecificMethods() },
        };
        ShardingDatabaseProviders = new Dictionary<string, IDatabaseSpecificMethods>
        {
            { "SqlServer", new SqlServerDatabaseSpecificMethods() },
            { "PostgreSQL", new PostgresDatabaseSpecificMethods() },
            { "SqliteInMemory", new StubSqliteDatabaseSpecificMethods() },
        };

        _caller = caller;
    }

    public List<ShardingEntry> GetAllPossibleShardingData()
    {
        return new List<ShardingEntry>
        {
            new ShardingEntry{Name = "Default Database", ConnectionName = "UnitTestConnection"},
            new ShardingEntry{Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "UnitTestConnection"},
            new ShardingEntry{Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = "Postgres"}
        };
    }

    public IEnumerable<string> GetConnectionStringNames()
    {
        return new[] { "UnitTestConnection", "PostgreSqlConnection" };
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

    public Task<List<(string databaseInfoName, bool? hasOwnDb, List<string> tenantNames)>> GetDatabaseInfoNamesWithTenantNamesAsync()
    {
        return Task.FromResult(new List<(string key, bool? hasOwnDb, List<string> tenantNames)>
        {
            ("Default Database", false, new List<string>{ "Tenant1","Tenant3"}),
            ("Other Database", true, new List<string>{ "Tenant2"}),
            ("PostgreSql1", null, new List<string>())
        });
    }
}