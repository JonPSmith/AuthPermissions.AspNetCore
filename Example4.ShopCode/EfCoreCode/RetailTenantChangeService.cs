// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Example4.ShopCode.AppStart;
using Example4.ShopCode.EfCoreClasses;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Example4.ShopCode.EfCoreCode
{
    public class RetailTenantChangeService : ITenantChangeService
    {

        public DbContext GetNewInstanceOfAppContext(SqlConnection sqlConnection)
        {
            var options = new DbContextOptionsBuilder<RetailDbContext>()
                .UseSqlServer(sqlConnection, dbOptions =>
                    dbOptions.MigrationsHistoryTable(StartupExtensions.RetailDbContextHistoryName))
                .Options;

            return new RetailDbContext(options, null);
        }

        /// <summary>
        /// This is called within a transaction to allow the the application-side of the database to either
        /// a) delete all the application-side data with the given DataKey, or b) list the changes to show to the admin user
        /// Notes:
        /// - The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// - When working in an hierarchical tenants you can get multiple calls, starting with the lower levels first
        /// 
        /// This implementation deletes the stock / sales rows with the DataKey and the app tenant, <see cref="RetailOutlet"/>
        /// </summary>
        /// <param name="appTransactionContext">The application's DbContext within a transaction</param>
        /// <param name="dataKey">The DataKey of the tenant</param>
        /// <param name="tenantId">The TenantId of the tenant</param>
        /// <param name="fullTenantName">The full name of the tenant</param>
        /// <returns>Returns null if all OK, otherwise the delete is rolled back and the return string is shown to the user</returns>
        public async Task<string> HandleTenantDeleteAsync(DbContext appTransactionContext, string dataKey, int tenantId, string fullTenantName)
        {
            //Higher hierarchical levels don't have data in this example, so it only tries to delete data if there is a RetailOutlet
            var retailOutletToDelete =
                await appTransactionContext.Set<RetailOutlet>()
                    .IgnoreQueryFilters().SingleOrDefaultAsync(x => x.AuthPTenantId == tenantId);
            if (retailOutletToDelete != null)
            {
                //yes, its a shop so delete all the stock / sales 
                var deleteSalesSql = $"DELETE FROM retail.{nameof(RetailDbContext.ShopSales)} WHERE DataKey = '{dataKey}'";
                await appTransactionContext.Database.ExecuteSqlRawAsync(deleteSalesSql);
                var deleteStockSql = $"DELETE FROM retail.{nameof(RetailDbContext.ShopStocks)} WHERE DataKey = '{dataKey}'";
                await appTransactionContext.Database.ExecuteSqlRawAsync(deleteStockSql);

                appTransactionContext.Remove(retailOutletToDelete); //finally delete the RetailOutlet
                await appTransactionContext.SaveChangesAsync();
            }

            return null; //null means OK, otherwise the delete is rolled back and the return string is shown to the user
        }

        /// <summary>
        /// This is called when the name of your Tenants is changed. This is useful if you use the tenant name in your multi-tenant data.
        /// NOTE: The created application's DbContext won't have a DataKey, so you will need to use IgnoreQueryFilters on any EF Core read
        /// </summary>
        /// <param name="appTransactionContext">The application's DbContext within a transaction</param>
        /// <param name="dataKey">The DataKey of the tenant</param>
        /// <param name="tenantId">The TenantId of the tenant</param>
        /// <param name="fullTenantName">The full name of the tenant</param>
        /// <returns>Returns null if all OK, otherwise the delete is rolled back and the return string is shown to the user</returns>
        public async Task<string> HandleUpdateNameAsync(DbContext appTransactionContext, string dataKey, int tenantId,
            string fullTenantName)
        {
            //Higher hierarchical levels don't have data in this example, so it only tries to delete data if there is a RetailOutlet
            var retailOutletToUpdate =
                await appTransactionContext.Set<RetailOutlet>()
                    .IgnoreQueryFilters().SingleOrDefaultAsync(x => x.AuthPTenantId == tenantId);

            retailOutletToUpdate.UpdateNames(fullTenantName);
            await appTransactionContext.SaveChangesAsync();

            return null;
        }
    }
}