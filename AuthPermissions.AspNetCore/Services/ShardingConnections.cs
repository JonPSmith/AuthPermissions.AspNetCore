// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.AspNetCore.Components.Forms;
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
    /// <param name="optionsAccessor">Dynamically assesses the ConnectionSting part of the appsetting file</param>
    /// <param name="context">AuthP context - used in <see cref="GetConnectionStringsWithNumTenantsAsync"/> method</param>
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
    /// This returns all the connection string names, with a list of tenant name  linked to each connection name
    /// </summary>
    /// <returns>List of connectionName+tenants using that connectionName</returns>
    public async Task<List<(string connectionName, List<string> tenantNames)>> GetConnectionStringsWithTenantNamesAsync()
    {
        var nameAndConnectionName  = await _context.Tenants
            .Select(x => new { x.ConnectionName, x.TenantFullName})
            .ToListAsync();
            
        var grouped = nameAndConnectionName.GroupBy(x => x.ConnectionName)
            .ToDictionary(x => x.Key,
                y => y.Select(z => z.TenantFullName));

        var result = new List<(string connectionName, List<string>)>();
        //Add connection string names that have no tenants in them so that you can see all the connection string  names
        foreach (var key in _connectionDict.Keys)
        {
            result.Add(grouped.ContainsKey(key)
                ? (key, grouped[key].ToList())
                : (key, new List<string>()));
        }

        return result;
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