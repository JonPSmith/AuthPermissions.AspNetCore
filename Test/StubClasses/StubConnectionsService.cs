// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode.SetupCode;
using LocalizeMessagesAndErrors.UnitTestingCode;
using StatusGeneric;
using Test.TestHelpers;
using TestSupport.Helpers;

namespace Test.StubClasses;

public class StubConnectionsService : IShardingConnections
{
    private readonly object _caller;

    /// <summary>
    /// This contains the methods with are specific to a database provider
    /// </summary>
    public IReadOnlyDictionary<string, IDatabaseSpecificMethods> DatabaseProviderMethods { get; }

    /// <summary>
    /// This returns the supported database provider that can be used for multi tenant sharding
    /// </summary>
    public string[] SupportedDatabaseProviders => DatabaseProviderMethods.Keys.ToArray();

    public StubConnectionsService(object caller)
    {
        DatabaseProviderMethods = new Dictionary<string, IDatabaseSpecificMethods>
        {
            { nameof(AuthPDatabaseTypes.SqlServer), new SqlServerDatabaseSpecificMethods("en".SetupAuthPLoggingLocalizer()) },
            { nameof(AuthPDatabaseTypes.Postgres), new PostgresDatabaseSpecificMethods("en".SetupAuthPLoggingLocalizer()) },
            { nameof(AuthPDatabaseTypes.SqliteInMemory), new StubSqliteInMemoryDatabaseSpecificMethods() },
        };
        _caller = caller;
    }

    public List<DatabaseInformation> GetAllPossibleShardingData()
    {
        return new List<DatabaseInformation>
        {
            new DatabaseInformation{Name = "Default Database", ConnectionName = "UnitTestConnection"},
            new DatabaseInformation{Name = "Other Database", DatabaseName = "MyDatabase1", ConnectionName = "UnitTestConnection"},
            new DatabaseInformation{Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = "Postgres"}
        };
    }

    public IEnumerable<string> GetConnectionStringNames()
    {
        return new[] { "UnitTestConnection", "PostgreSqlConnection" };
    }

    public IStatusGeneric TestFormingConnectionString(DatabaseInformation databaseInfo)
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