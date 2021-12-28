// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This is the interface for the creating, deleting, updating, or hierarchical moving of tenants
    /// This allows the changes to the AuthP's Tenant to be applied to the application's tenant data within a transaction.
    /// This means if either the AuthP's Tenant, or the application's tenant data fails, then both changes will be rolled back
    /// </summary>
    public interface ITenantChangeService
    {
        /// <summary>
        /// This creates an instance of the application's DbContext to use within an transaction with the AuthPermissionsDbContext
        /// </summary>
        DbContext GetNewInstanceOfAppContext(SqlConnection sqlConnection);

        /// <summary>
        /// When a new AuthP Tenant is created, then this method is called. If you have a tenant-type entity in your
        /// application's database, then this allows you to create a new entity for the new tenant
        /// NOTE: With hierarchical tenants you cannot be sure that the tenant has, or will have, children
        /// </summary>
        /// <param name="appTransactionContext">The application's DbContext within a transaction</param>
        /// <param name="dataKey">The DataKey of the tenant being deleted</param>
        /// <param name="tenantId">The TenantId of the tenant being deleted</param>
        /// <param name="fullTenantName">The full name of the tenant being deleted</param>
        /// <returns>Returns null if all OK, otherwise the create is rolled back and the return string is shown to the user</returns>
        Task<string> CreateNewTenantAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName);

        /// <summary>
        /// This is called within a transaction to allow the the application-side of the database to either
        /// a) delete all the application-side data with the given DataKey, or b) list the changes to show to the admin user
        /// Notes:
        /// - The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// - When working in an hierarchical tenants you can get multiple calls, starting with the lower levels first
        /// </summary>
        /// <param name="appTransactionContext">The application's DbContext within a transaction</param>
        /// <param name="dataKey">The DataKey of the tenant being deleted</param>
        /// <param name="tenantId">The TenantId of the tenant being deleted</param>
        /// <param name="fullTenantName">The full name of the tenant being deleted</param>
        /// <returns>Returns null if all OK, otherwise the delete is rolled back and the return string is shown to the user</returns>
        Task<string> HandleTenantDeleteAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName);

        /// <summary>
        /// This is called when the name of your Tenants is changed. This is useful if you use the tenant name in your multi-tenant data.
        /// NOTE: The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// </summary>
        /// <param name="appTransactionContext">The application's DbContext within a transaction</param>
        /// <param name="dataKey">The DataKey of the tenant</param>
        /// <param name="tenantId">The TenantId of the tenant</param>
        /// <param name="fullTenantName">The full name of the tenant</param>
        /// <returns>Returns null if all OK, otherwise the name change is rolled back and the return string is shown to the user</returns>
        Task<string> HandleUpdateNameAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName);

        /// <summary>
        /// This is used with hierarchical tenants, where you move one tenant (and its children) to another tenant
        /// This requires you to change the DataKeys of each application's tenant data, so they link to the new tenant.
        /// Also, if you contain the name of the tenant in your data, then you need to update its new FullName
        /// Notes:
        /// - The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// - You can get multiple calls if move a higher level
        /// </summary>
        /// <param name="appTransactionContext"></param>
        /// <param name="oldDataKey">The old DataKey to look for</param>
        /// <param name="newDataKey">The new DataKey to change to</param>
        /// <param name="tenantId">The TenantId of the tenant being moved</param>
        /// <param name="newFullTenantName">The new full name of the tenant</param>
        /// <returns>Returns null if all OK, otherwise the move is rolled back and the return string is shown to the user</returns>
        Task<string> MoveHierarchicalTenantDataAsync(DbContext appTransactionContext, string oldDataKey, string newDataKey,
            int tenantId, string newFullTenantName);

    }
}