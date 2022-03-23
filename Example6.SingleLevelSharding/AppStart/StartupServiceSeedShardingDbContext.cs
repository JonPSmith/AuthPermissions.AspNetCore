// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode;
using Example6.SingleLevelSharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;

namespace Example6.SingleLevelSharding.AppStart
{
    /// <summary>
    /// If there are no RetailOutlets in the RetailDbContext it seeds the RetailDbContext with RetailOutlets and gives each of them some stock
    /// </summary>

    public class StartupServiceSeedShardingDbContext : IStartupServiceToRunSequentially
    {
        public int OrderNum { get; } //runs after migration of the ShardingSingleDbContext

        public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
        {
            var context = scopedServices.GetRequiredService<ShardingSingleDbContext>();
            var numInvoices = await context.Invoices.IgnoreQueryFilters().CountAsync();
            if (numInvoices == 0)
            {
                var authTenantAdmin = scopedServices.GetRequiredService<IAuthTenantAdminService>();

                var seeder = new SeedShardingDbContext(context);
                await seeder.SeedInvoicesForAllTenantsAsync(authTenantAdmin.QueryTenants().ToArray());
            }
        }
    }
}