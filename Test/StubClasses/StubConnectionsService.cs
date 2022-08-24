// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AspNetCore.Services;
using Npgsql.PostgresTypes;
using StatusGeneric;
using TestSupport.Helpers;

namespace Test.StubClasses;

public class StubConnectionsService : IShardingConnections
{
    private readonly object _caller;
    private readonly bool _badConnectionString;

    public StubConnectionsService(object caller, bool badConnectionString = false)
    {
        _caller = caller;
        _badConnectionString = badConnectionString;
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

    public string[] GetSupportedDatabaseTypes()
    {
        return new string[] { "SqlServer", "Postgres" };
    }

    public IStatusGeneric TestFormingConnectionString(DatabaseInformation databaseInfo)
    {
        var status = new StatusGenericHandler();
        if (_badConnectionString)
            status.AddError("Bad connection string");
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