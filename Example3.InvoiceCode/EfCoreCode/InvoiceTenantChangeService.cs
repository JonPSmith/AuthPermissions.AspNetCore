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
        public DbContext GetNewInstanceOfAppContext(SqlConnection sqlConnection)
        {
            var options = new DbContextOptionsBuilder<InvoicesDbContext>()
                .UseSqlServer(sqlConnection, dbOptions =>
                    dbOptions.MigrationsHistoryTable(StartupExtensions.InvoicesDbContextHistoryName))
                .Options;

            return new InvoicesDbContext(options, null);
        }

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

        public async Task<string> HandleTenantDeleteAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName)
        {
            var deleteSalesSql = $"DELETE FROM invoice.{nameof(InvoicesDbContext.LineItems)} WHERE DataKey = '{dataKey}'";
            await appTransactionContext.Database.ExecuteSqlRawAsync(deleteSalesSql);
            var deleteStockSql = $"DELETE FROM invoice.{nameof(InvoicesDbContext.Invoices)} WHERE DataKey = '{dataKey}'";
            await appTransactionContext.Database.ExecuteSqlRawAsync(deleteStockSql);

            var companyTenant = await appTransactionContext.Set<CompanyTenant>()
                .SingleOrDefaultAsync(x => x.AuthPTenantId == tenantId);
            if (companyTenant != null)
            {
                appTransactionContext.Remove(companyTenant);
                await appTransactionContext.SaveChangesAsync();
            }

            return null;
        }

        public async Task<string> HandleUpdateNameAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName)
        {
            var companyTenant = await appTransactionContext.Set<CompanyTenant>()
                .SingleOrDefaultAsync(x => x.AuthPTenantId == tenantId);
            if (companyTenant != null)
            {
                companyTenant.CompanyName = fullTenantName;
                await appTransactionContext.SaveChangesAsync();
            }

            return null;
        }

        public Task<string> MoveHierarchicalTenantDataAsync(DbContext appTransactionContext, string oldDataKey, string newDataKey, int tenantId,
            string newFullTenantName)
        {
            //This example is using single level multi-tenant, so this will never be called.

            throw new System.NotImplementedException();
        }
    }
}