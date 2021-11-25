// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Example4.ShopCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RunMethodsSequentially;

namespace Example4.ShopCode.AppStart
{
    /// <summary>
    /// If there are no RetailOutlets in the RetailDbContext it seeds the RetailDbContext with RetailOutlets and gives each of them some stock
    /// </summary>

    public class StartupServiceServiceSeedRetailDatabase : IStartupServiceToRunSequentially
    {
        public int OrderNum { get; } //runs after the RetailDbContext has been migrated

        public async ValueTask ApplyYourChangeAsync(IServiceProvider scopedServices)
        {
            var context = scopedServices.GetRequiredService<RetailDbContext>();
            var numRetail = await context.RetailOutlets.IgnoreQueryFilters().CountAsync();
            if (numRetail == 0)
            {
                var authTenantAdmin = scopedServices.GetRequiredService<IAuthTenantAdminService>();

                var service = new SeedShopsOnStartup(context, authTenantAdmin);
                await service.CreateShopsAndSeedStockAsync(SeedShopsOnStartup.SeedStockText);
            }
        }
    }
}