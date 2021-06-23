// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using StatusGeneric;

namespace AuthPermissions.AdminCode
{
    public interface IAuthTenantAdminService
    {
        /// <summary>
        /// This simply returns a IQueryable of Tenants
        /// </summary>
        /// <returns>query on the database</returns>
        IQueryable<Tenant> QueryTenants();

        /// <summary>
        /// This adds a new, non-Hierarchical Tenant
        /// </summary>
        /// <param name="tenantName">Name of the new single-level tenant (must be unique)</param>
        /// <returns>A status with any errors found</returns>
        Task<IStatusGeneric> AddSingleTenantAsync(string tenantName);

        /// <summary>
        /// This adds a new Hierarchical Tenant, liking it into the parent (which can be null)
        /// </summary>
        /// <param name="thisLevelTenantName">Name of the new tenant. This will be prefixed with the parent's tenant name to make it unique</param>
        /// <param name="parentTenantName">The name of the parent that this tenant </param>
        /// <returns>A status with any errors found</returns>
        Task<IStatusGeneric> AddHierarchicalTenantAsync(string thisLevelTenantName, string parentTenantName);

        /// <summary>
        /// This updates the name of this tenant to the <see param="newTenantLevelName"/>.
        /// This also means all the children underneath need to have their full name updated too
        /// </summary>
        /// <param name="fullTenantName"></param>
        /// <param name="newTenantLevelName"></param>
        /// <returns></returns>
        Task<IStatusGeneric> UpdateTenantNameAsync(string fullTenantName, string newTenantLevelName);

        /// <summary>
        /// This moves a hierarchical tenant to a new parent (which might be null)
        /// This changes the TenantName and the TenantDataKey of the selected tenant and all of its children
        /// </summary>
        /// <param name="fullTenantName">The full name of the tenant to move to another parent </param>
        /// <param name="newParentFullName">he full name of the new parent tenant (can be null, in which case the tenant moved to the top level</param>
        /// <returns></returns>
        Task<IStatusGeneric> MoveHierarchicalTenantToAnotherParentAsync(string fullTenantName, string newParentFullName);
    }
}