// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthPermissions.AspNetCore.Services;

/// <summary>
/// This is used to get all the connection strings
/// </summary>
public class ConnectionStringsOption : Dictionary<string, string> { }

/// <summary>
/// This service reads in connection strings  
/// </summary>
public class ShardingConnections : IShardingConnections
{
    private readonly ConnectionStringsOption _connectionDict;
    private readonly AuthPermissionsDbContext _context;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="optionsAccessor"></param>
    public ShardingConnections(IOptionsSnapshot<ConnectionStringsOption> optionsAccessor, AuthPermissionsDbContext context)
    {
        //thanks to https://stackoverflow.com/questions/37287427/get-multiple-connection-strings-in-appsettings-json-without-ef
        _connectionDict = optionsAccessor.Value;

        _context = context;
    }

    /// <summary>
    /// This returns all the connection string names currently in the application's appsettings
    /// </summary>
    /// <returns>The name of each connection string</returns>
    public IEnumerable<string> GetAllConnectionStringNames()
    {
        return _connectionDict.Keys;
    }

    /// <summary>
    /// This returns all the connection string names, with the number of tenants linked to those connection string names
    /// </summary>
    /// <returns>List of KeyValuePair(string, int) ordered by the connection name</returns>
    public async Task<IEnumerable<KeyValuePair<string, int>>> GetConnectionStringsWithNumTenantsAsync()
    {
        var grouped = await _context.Tenants.GroupBy(x => x.ConnectionName)
            .Select(x => new KeyValuePair<string, int>(x.Key, x.Count()))
            .ToListAsync();

        foreach (var key in _connectionDict.Keys)
        {
            if (grouped.All(x => x.Key != key))
                grouped.Add(new KeyValuePair<string, int>(key,0));
        }

        return grouped.OrderBy(x => x.Key);
    }

    /// <summary>
    /// This will provide the connection string for the entry with the given connection string name
    /// </summary>
    /// <param name="connectionName">The name of the connection string you want to access</param>
    /// <returns>The connection string, or null if not found</returns>
    public string GetNamedConnectionString(string connectionName)
    {
        return _connectionDict.ContainsKey(connectionName) 
        ? _connectionDict[connectionName]
        : null;
    }
}