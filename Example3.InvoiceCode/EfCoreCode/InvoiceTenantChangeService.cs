// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.EfCoreClasses;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Example3.InvoiceCode.EfCoreCode
{
    public class InvoiceTenantChangeService : ITenantChangeService
    {
        /// <summary>
        /// This creates an instance of the application's DbContext to use within an transaction with the AuthPermissionsDbContext
        /// </summary>
        public DbContext GetNewInstanceOfAppContext(SqlConnection sqlConnection)
        {
            var options = new DbContextOptionsBuilder<InvoicesDbContext>()
                .UseSqlServer(sqlConnection, dbOptions =>
                    dbOptions.MigrationsHistoryTable(StartupExtensions.InvoicesDbContextHistoryName))
                .Options;

            return new InvoicesDbContext(options, null);
        }

        /// <summary>
        /// When a new AuthP Tenant is created, then this method is called. If you have a tenant-type entity in your
        /// application's database, then this allows you to create a new entity for the new tenant
        /// </summary>
        /// <param name="appTransactionContext">The application's DbContext within a transaction</param>
        /// <param name="dataKey">The DataKey of the tenant being deleted</param>
        /// <param name="tenantId">The TenantId of the tenant being deleted</param>
        /// <param name="fullTenantName">The full name of the tenant being deleted</param>
        /// <returns>Returns null if all OK, otherwise the create is rolled back and the return string is shown to the user</returns>
        public async Task<string> CreateNewTenantAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName)
        {
            var newCompanyTenant = new CompanyTenant
            {
                DataKey = dataKey,
                AuthPTenantId = tenantId,
                CompanyName = fullTenantName
            };
            appTransactionContext.Add(newCompanyTenant);
            await appTransactionContext.SaveChangesAsync();

            return null;
        }

        /// <summary>
        /// This is called within a transaction to allow the the application-side of the database to either
        /// a) delete all the application-side data with the given DataKey, or b) list the changes to show to the admin user
        /// Note: The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// </summary>
        /// <param name="appTransactionContext">The application's DbContext within a transaction</param>
        /// <param name="dataKey">The DataKey of the tenant being deleted</param>
        /// <param name="tenantId">The TenantId of the tenant being deleted</param>
        /// <param name="fullTenantName">The full name of the tenant being deleted</param>
        /// <returns>Returns null if all OK, otherwise the delete is rolled back and the return string is shown to the user</returns>
        public async Task<string> HandleTenantDeleteAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName)
        {
            var deleteSalesSql = $"DELETE FROM invoice.{nameof(InvoicesDbContext.LineItems)} WHERE DataKey = '{dataKey}'";
            await appTransactionContext.Database.ExecuteSqlRawAsync(deleteSalesSql);
            var deleteStockSql = $"DELETE FROM invoice.{nameof(InvoicesDbContext.Invoices)} WHERE DataKey = '{dataKey}'";
            await appTransactionContext.Database.ExecuteSqlRawAsync(deleteStockSql);

            var companyTenant = await appTransactionContext.Set<CompanyTenant>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.AuthPTenantId == tenantId);
            if (companyTenant != null)
            {
                appTransactionContext.Remove(companyTenant);
                await appTransactionContext.SaveChangesAsync();
            }

            return null;
        }

        /// <summary>
        /// This is called when the name of your Tenants is changed. This is useful if you use the tenant name in your multi-tenant data.
        /// NOTE: The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// </summary>
        /// <param name="appTransactionContext">The application's DbContext within a transaction</param>
        /// <param name="dataKey">The DataKey of the tenant</param>
        /// <param name="tenantId">The TenantId of the tenant</param>
        /// <param name="fullTenantName">The full name of the tenant</param>
        /// <returns>Returns null if all OK, otherwise the tenant name is rolled back and the return string is shown to the user</returns>
        public async Task<string> HandleUpdateNameAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName)
        {
            var companyTenant = await appTransactionContext.Set<CompanyTenant>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(x => x.AuthPTenantId == tenantId);
            if (companyTenant != null)
            {
                companyTenant.CompanyName = fullTenantName;
                await appTransactionContext.SaveChangesAsync();
            }

            return null;
        }

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
        public Task<string> MoveHierarchicalTenantDataAsync(DbContext appTransactionContext, string oldDataKey, string newDataKey, int tenantId,
            string newFullTenantName)
        {
            //This example is using single level multi-tenant, so this will never be called.

            throw new System.NotImplementedException();
        }
    }
}