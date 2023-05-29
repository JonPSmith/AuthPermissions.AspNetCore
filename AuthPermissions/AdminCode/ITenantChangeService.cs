// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode.DataLayer.Classes;

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
        /// <param name="tenant">The tenant data used to create a new tenant</param>
        /// <returns>Returns null if all OK, otherwise the create is rolled back and the return string is shown to the user</returns>
        Task<string> CreateNewTenantAsync(Tenant tenant);

        //----------------------------------------------------
        // Single-level only methods

        /// <summary>
        /// This is called when the name of your single-level tenant is changed. This is useful if you use the tenant name in your multi-tenant data.
        /// NOTE: The application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back.
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns>Returns null if all OK, otherwise the name change is rolled back and the return string is shown to the user</returns>
        Task<string> SingleTenantUpdateNameAsync(Tenant tenant);

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
        /// <param name="tenant"></param>
        /// <returns>Returns null if all OK, otherwise the AuthP part of the delete is rolled back and the return string is shown to the user</returns>
        Task<string> SingleTenantDeleteAsync(Tenant tenant);

        //------------------------------------------------
        // Hierarchical only methods

        /// <summary>
        /// This is called when the name of your Hierarchical tenants is changed. This is useful if you use the tenant name in your multi-tenant data.
        /// NOTE: The application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read.
        /// You should apply multiple changes within a transaction so that if any fails then any previous changes will be rolled back.
        /// </summary>
        /// <param name="tenantsToUpdate">This contains the tenants to update.</param>
        /// <returns>Returns null if all OK, otherwise the name change is rolled back and the return string is shown to the user</returns>
        Task<string> HierarchicalTenantUpdateNameAsync(List<Tenant> tenantsToUpdate);

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
        Task<string> MoveHierarchicalTenantDataAsync(List<(string oldDataKey, Tenant tenantToMove)> tenantToUpdate);

        //-------------------------------------------------
        // Sharding only method

        /// <summary>
        /// This is called when a tenant is moved to a new database setting.
        /// Its job is to move all the application's data to a new database (which isn't an easy thing to do!)
        /// and then delete the old data
        /// This method is called for both a single-level or hierarchical tenant, but the code for each is quite different.
        /// NOTE: If its a hierarchical tenant, then the tenant will be the highest parent.
        /// NOTE: If the tenant's <see cref="Tenant.HasOwnDb"/> is true, then its worth you checking the database
        /// doesn't have any application's data in the new database. This is especially important for a single-level tenant
        /// because the query filter will be turned off and any other data would be returned.
        /// </summary>
        /// <param name="oldDatabaseInfoName">The connection string to the old database</param>
        /// <param name="oldDataKey"></param>
        /// <param name="updatedTenant">This tenant has had its sharding information updated</param>
        /// <returns></returns>
        Task<string> MoveToDifferentDatabaseAsync(string oldDatabaseInfoName, string oldDataKey, Tenant updatedTenant);

    }
}