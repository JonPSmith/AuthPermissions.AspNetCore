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


    public IEnumerable<string> GetAllConnectionStringNames()
    {
        return new[] { "DefaultConnection", "OtherConnection" };
    }

    public Task<List<(string connectionName, List<string> tenantNames)>> GetConnectionStringsWithTenantNamesAsync()
    {
        return Task.FromResult( new List<(string key, List<string> tenantNames)>
        {
            ("DefaultConnection", new List<string>{ "Tenant1, Tenant3"}),
            ("OtherConnection", new List<string>{ "Tenant2"})
        });
    }

    public string GetNamedConnectionString(string connectionName)
    {
        return connectionName switch
        {
            "DefaultConnection" => _caller.GetUniqueDatabaseConnectionString("main"),
            "OtherConnection" => _caller.GetUniqueDatabaseConnectionString("other"),
            _ => null
        };
    }
}