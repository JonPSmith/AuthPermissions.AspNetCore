// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AuthPermissions.AdminCode
{
    /// <summary>
    /// This is the interface for the deleting, updating, or hierarchical moving of tenants
    /// This allows the changes to the AuthP's Tenant to be applied to the application's tenant data within a transaction.
    /// This means if either the AuthP's Tenant, or the application's tenant data fails, then both changes will be rolled back
    /// </summary>
    public interface ITenantChangeService
    {
        /// <summary>
        /// This is the application's DbContext to use within an transaction with the AuthPermissionsDbContext
        /// </summary>
        DbContext GetNewInstanceOfAppContext(SqlConnection sqlConnection);

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
        /// <returns>Returns null if all OK, otherwise the delete is rolled back and the return string is shown to the user</returns>
        Task<string> HandleUpdateNameAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName);
    }
}