// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.Services;
using TestSupport.Helpers;

namespace Test.TestHelpers;

public class StubConnectionsService : IShardingConnections
{
    private readonly object _caller;

    public StubConnectionsService(object caller)
    {
        _caller = caller;
    }

    public List<ShardingDatabaseData> GetAllPossibleShardingData()
    {
        return new List<ShardingDatabaseData>
        {
            new ShardingDatabaseData{Name = "Default Database", ConnectionName = "UnitTestConnection"},
            new ShardingDatabaseData{Name = "OtherConnection", ConnectionName = "UnitTestConnection"},
            new ShardingDatabaseData{Name = "PostgreSql1", ConnectionName = "PostgreSqlConnection", DatabaseName = "StubTest", DatabaseType = "Postgres"}
        };
    }

    public string FormConnectionString(string databaseInfoName)
    {
        return databaseInfoName switch
        {
            "Default Database" => _caller.GetUniqueDatabaseConnectionString("main"),
            "OtherConnection" => _caller.GetUniqueDatabaseConnectionString("other"),
            "PostgreSql1" => _caller.GetUniquePostgreSqlConnectionString(),
            _ => null
        };
    }

    public Task<List<(string databaseInfoName, List<string> tenantNames)>> GetDatabaseInfoNamesWithTenantNamesAsync()
    {
        return Task.FromResult( new List<(string key, List<string> tenantNames)>
        {
            ("Default Database", new List<string>{ "Tenant1, Tenant3"}),
            ("OtherConnection", new List<string>{ "Tenant2"}),
            ("PostgreSql1", new List<string>())
        });
    }
}