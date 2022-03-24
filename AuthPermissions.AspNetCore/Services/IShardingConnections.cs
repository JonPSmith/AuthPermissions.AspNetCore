// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthPermissions.AspNetCore.Services;

/// <summary>
/// The interface for the service to manage the connection strings in the appsetting file.
/// </summary>
public interface IShardingConnections
{
    /// <summary>
    /// This returns all the connection strings name in the application's appsettings
    /// </summary>
    /// <returns>The name of each connection string</returns>
    IEnumerable<string> GetAllConnectionStringNames();

    /// <summary>
    /// This returns all the connection string names, with the number of tenants linked to those connection string names
    /// </summary>
    /// <returns>List of KeyValuePair(string, int) ordered by the connection name</returns>
    Task<IEnumerable<KeyValuePair<string, int>>> GetConnectionStringsWithNumTenantsAsync();

    /// <summary>
    /// This will provide the connection string for the entry with the given connection string name
    /// </summary>
    /// <param name="connectionName">The name of the connection string you want to access</param>
    /// <returns>The connection string, or null if not found</returns>
    string GetNamedConnectionString(string connectionName);
}