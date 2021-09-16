// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using StatusGeneric;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// interface of the AuthP Tenant admin services
    /// </summary>
    public interface IAuthTenantAdminService
    {
        /// <summary>
        /// This simply returns a IQueryable of Tenants
        /// </summary>
        /// <returns>query on the AuthP database</returns>
        IQueryable<Tenant> QueryTenants();

        /// <summary>
        /// This query returns all the end leaf Tenants, which is the bottom of the hierarchy (i.e. no children below it)
        /// </summary>
        /// <returns>query on the AuthP database</returns>
        IQueryable<Tenant> QueryEndLeafTenants();

        /// <summary>
        /// This returns a tenant, with its Parent but no children, that has the given TenantId
        /// </summary>
        /// <param name="tenantId">primary key of the tenant you are looking for</param>
        /// <returns>Status. If successful, then contains the Tenant</returns>
        Task<IStatusGeneric<Tenant>> GetTenantViaIdAsync(int tenantId);

        /// <summary>
        /// This returns a list of all the child tenants
        /// </summary>
        /// <param name="tenantId">primary key of the tenant you are looking for</param>
        /// <returns>A list of child tenants for this tenant (can be empty)</returns>
        Task<List<Tenant>> GetHierarchicalTenantChildrenViaIdAsync(int tenantId);

        /// <summary>
        /// This adds a new, single level  Tenant
        /// </summary>
        /// <param name="tenantName">Name of the new single-level tenant (must be unique)</param>
        /// <returns>A status with any errors found</returns>
        Task<IStatusGeneric> AddSingleTenantAsync(string tenantName);

        /// <summary>
        /// This adds a new Hierarchical Tenant, liking it into the parent (which can be null)
        /// </summary>
        /// <param name="tenantName">Name of the new tenant. This will be prefixed with the parent's tenant name to make it unique</param>
        /// <param name="parentTenantId">The primary key of the parent. If 0 then the new tenant is at the top level</param>
        /// <returns>A status with any errors found</returns>
        Task<IStatusGeneric> AddHierarchicalTenantAsync(string tenantName, int parentTenantId);

        /// <summary>
        /// This updates the name of this tenant to the <see param="newTenantLevelName"/>.
        /// This also means all the children underneath need to have their full name updated too
        /// This method uses the <see cref="ITenantChangeService"/> you provided via the <see cref="RegisterExtensions.RegisterTenantChangeService{TTenantChangeService}"/>
        /// to update the application's tenant data. You also need to set the <see cref="AuthPermissionsOptions.AppConnectionString"/> in the options.
        /// </summary>
        /// <param name="tenantId">Primary key of the tenant to change</param>
        /// <param name="newTenantName">This is the new name for this tenant name</param>
        /// <returns></returns>
        Task<IStatusGeneric> UpdateTenantNameAsync(int tenantId, string newTenantName);

        /// <summary>
        /// This moves a hierarchical tenant to a new parent (which might be null). This changes the TenantFullName and the
        /// TenantDataKey of the selected tenant and all of its children
        /// This method uses the <see cref="ITenantChangeService"/> you provided via the <see cref="RegisterExtensions.RegisterTenantChangeService{TTenantChangeService}"/>
        /// to move the application's tenant data. You also need to set the <see cref="AuthPermissionsOptions.AppConnectionString"/> in the options.
        /// </summary>
        /// <param name="tenantToMoveId">The primary key of the AuthP tenant to move</param>
        /// <param name="newParentTenantId">Primary key of the new parent, if 0 then you move the tenant to top</param>
        /// <returns>status</returns>
        Task<IStatusGeneric> MoveHierarchicalTenantToAnotherParentAsync(int tenantToMoveId, int newParentTenantId);

        /// <summary>
        /// This will delete the tenant (and all its children if the data is hierarchical) and uses the <see cref="ITenantChangeService"/>,
        /// but only if no AuthP user are linked to this tenant (it will return errors listing all the AuthP user that are linked to this tenant
        /// This method uses the <see cref="ITenantChangeService"/> you provided via the <see cref="RegisterExtensions.RegisterTenantChangeService{TTenantChangeService}"/>
        /// to delete the application's tenant data.  You also need to set the <see cref="AuthPermissionsOptions.AppConnectionString"/> in the options.
        /// </summary>
        /// <returns>Status returning the <see cref="ITenantChangeService"/> service, in case you want copy the delete data instead of deleting</returns>
        Task<IStatusGeneric<ITenantChangeService>> DeleteTenantAsync(int tenantId);
    }
}