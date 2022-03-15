// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.DataLayer.Classes;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This is the interface for the creating, deleting, updating, or hierarchical moving of tenants
    /// This service should apply changes to the application's database. 
    /// The methods are called by the <see cref="AuthTenantAdminService"/> methods withing a transaction,
    /// so that if the application database changes fails, then the AuthP changes will be rolled back.
    /// </summary>
    public interface ITenantChangeService
    {
        /// <summary>
        /// When a new AuthP Tenant is created, then this method is called. If you have a tenant-type entity in your
        /// application's database, then this allows you to create a new entity for the new tenant.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back.
        /// NOTE: With hierarchical tenants you cannot be sure that the tenant has, or will have, children
        /// </summary>
        /// <param name="dataKey">The DataKey of the tenant being deleted</param>
        /// <param name="tenantId">The TenantId of the tenant being deleted</param>
        /// <param name="fullTenantName">The full name of the tenant being deleted</param>
        /// <returns>Returns null if all OK, otherwise the create is rolled back and the return string is shown to the user</returns>
        Task<string> CreateNewTenantAsync(string dataKey, int tenantId, string fullTenantName);

        /// <summary>
        /// This is called when the name of your Tenants is changed. This is useful if you use the tenant name in your multi-tenant data.
        /// NOTE: The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back.
        /// </summary>
        /// <param name="dataKey">The DataKey of the tenant</param>
        /// <param name="tenantId">The TenantId of the tenant</param>
        /// <param name="fullTenantName">The full name of the tenant</param>
        /// <returns>Returns null if all OK, otherwise the name change is rolled back and the return string is shown to the user</returns>
        Task<string> HandleUpdateNameAsync(string dataKey, int tenantId, string fullTenantName);

        /// <summary>
        /// This is used with single-level tenant to either
        /// a) delete all the application-side data with the given DataKey, or
        /// b) soft-delete the data.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back
        /// Notes:
        /// - The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// - You can provide information of what you have done by adding public parameters to this class.
        ///   The TenantAdmin <see cref="AuthTenantAdminService.DeleteTenantAsync"/> method returns your class on a successful Delete
        /// </summary>
        /// <param name="dataKey">The DataKey of the tenant being deleted</param>
        /// <param name="tenantId">The TenantId of the tenant being deleted</param>
        /// <param name="fullTenantName">The full name of the tenant being deleted</param>
        /// <returns>Returns null if all OK, otherwise the AuthP part of the delete is rolled back and the return string is shown to the user</returns>
        Task<string> SingleTenantDeleteAsync(string dataKey, int tenantId, string fullTenantName);

        /// <summary>
        /// This is used with hierarchical tenants to either
        /// a) delete all the application-side data with the given DataKey, or
        /// b) soft-delete the data.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back
        /// Notes:
        /// - The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// - You can provide information of what you have done by adding public parameters to this class.
        ///   The TenantAdmin <see cref="AuthTenantAdminService.DeleteTenantAsync"/> method returns your class on a successful Delete
        /// </summary>
        /// <param name="tenantsInOrder">The tenants to delete with the children first in case a higher level links to a lower level</param>
        /// <returns>Returns null if all OK, otherwise the AuthP part of the delete is rolled back and the return string is shown to the user</returns>
        Task<string> HierarchicalTenantDeleteAsync(List<Tenant> tenantsInOrder);

        /// <summary>
        /// This is used with hierarchical tenants, where you move one tenant (and its children) to another tenant
        /// This requires you to change the DataKeys of each application's tenant data, so they link to the new tenant.
        /// Also, if you contain the name of the tenant in your data, then you need to update its new FullName
        /// Notes:
        /// - The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// - You can get multiple calls if move a higher level
        /// </summary>
        /// <param name="tenantToUpdate">The data to update each tenant. This starts at the parent and then recursively works down the children</param>
        /// <returns>Returns null if all OK, otherwise AuthP part of the move is rolled back and the return string is shown to the user</returns>
        Task<string> MoveHierarchicalTenantDataAsync(
            List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)> tenantToUpdate);

    }
}