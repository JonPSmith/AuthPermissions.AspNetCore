// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using AuthPermissions.CommonCode;
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

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="optionsAccessor"></param>
    public ShardingConnections(IOptionsSnapshot<ConnectionStringsOption> optionsAccessor)
    {
        //thanks to https://stackoverflow.com/questions/37287427/get-multiple-connection-strings-in-appsettings-json-without-ef
        _connectionDict = optionsAccessor.Value;
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