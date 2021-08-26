// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
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
        /// <returns>query on the database</returns>
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
        /// This adds a new, non-Hierarchical Tenant
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
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="newTenantName"></param>
        /// <returns></returns>
        Task<IStatusGeneric> UpdateTenantNameAsync(int tenantId, string newTenantName);


        /// <summary>
        /// This moves a hierarchical tenant to a new parent (which might be null)
        /// This changes the TenantFullName and the TenantDataKey of the selected tenant and all of its children
        /// WARNING: If the tenants have data in your database, then you need to change their DataKey using the <see param="getOldNewDataKey"/> action.
        /// </summary>
        /// <param name="tenantToMoveId">Primary key of the tenant to move to another parent</param>
        /// <param name="parentTenantId">Primary key of the new parent, if 0 then you move the tenant to </param>
        /// <param name="getOldNewDataKey">This action is called at every tenant that is moved.
        /// This allows you to obtains the previous DataKey and the new DataKey of every tenant that was moved so that you can move the data</param>
        /// Providing an action will also stops SaveChangesAsync being called so that you can
        /// <returns>
        /// Returns a status, which has the current AuthPermissionsDbContext, if the <see param="getOldNewDataKey"/> is provided.
        /// This allows you to call the SaveChangesAsync within your 
        /// </returns>
        Task<IStatusGeneric<AuthPermissionsDbContext>> MoveHierarchicalTenantToAnotherParentAsync(
            int tenantToMoveId, int parentTenantId,
            Action<(string previousDataKey, string newDataKey)> getOldNewDataKey);

        /// <summary>
        /// This will delete the tenant (and all its children if the data is hierarchical
        /// WARNING: This method does NOT delete the data in your application. You need to do that using the DataKey returned in the status Result
        /// </summary>
        /// <param name="tenantId">The primary key of the tenant you want to </param>
        /// <param name="getDeletedTenantData">This action is called for each tenant that was deleted to tell you what has been deleted.
        /// You can either use the DataKeys to delete the multi-tenant data, 
        /// or don't delete the data (because it can't be accessed anyway) but show it to the admin user in case you want to re-link it to new tenants</param>
        /// <returns>Status</returns>
        Task<IStatusGeneric> DeleteTenantAsync(int tenantId,
            Action<(string fullTenantName, string dataKey)> getDeletedTenantData);
    }
}