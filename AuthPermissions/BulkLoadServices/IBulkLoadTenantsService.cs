// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using StatusGeneric;

namespace AuthPermissions.BulkLoadServices
{
    /// <summary>
    /// Bulk load multiple tenants
    /// </summary>
    public interface IBulkLoadTenantsService
    {
        /// <summary>
        /// This allows you to define tenants in a bulk load from a string. Each line in that string should hold a tenant
        /// (a line is ended with <see cref="Environment.NewLine"/>)
        /// If you are using a hierarchical tenant design, then you must define the higher company first
        /// </summary>
        /// <param name="linesOfText">If you are using a single layer then each line contains the a tenant name
        /// If you are using hierarchical tenant, then each line contains the whole hierarchy with '|' as separator, e.g.
        /// Holding company
        /// Holding company | USA branch 
        /// Holding company | USA branch | East Coast 
        /// Holding company | USA branch | East Coast | Washington
        /// Holding company | USA branch | East Coast | NewYork
        /// </param>
        /// <param name="options">The IAuthPermissionsOptions to check what type of tenant setting you have</param>
        /// <returns></returns>
        Task<IStatusGeneric> AddTenantsToDatabaseAsync(string linesOfText, IAuthPermissionsOptions options);
    }
}