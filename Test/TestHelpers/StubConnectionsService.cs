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

    public async Task<IEnumerable<KeyValuePair<string, int>>> GetConnectionStringsWithNumTenantsAsync()
    {
        return new List<KeyValuePair<string, int>>
        {
            new KeyValuePair<string, int>("DefaultConnection", 3),
            new KeyValuePair<string, int>("OtherConnection", 1)
        };
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