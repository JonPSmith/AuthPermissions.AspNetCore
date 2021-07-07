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

namespace Example4.ShopCode.AppStart
{
    /// <summary>
    /// If there are no RetailOutlets in the RetailDbContext it seeds the RetailDbContext with RetailOutlets and gives each of them some stock
    /// </summary>

    public class HostedServiceSeedRetailDatabase : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public HostedServiceSeedRetailDatabase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<RetailDbContext>();
                var numRetail = await context.RetailOutlets.IgnoreQueryFilters().CountAsync();
                if (numRetail == 0)
                {
                    var authTenantAdmin = services.GetRequiredService<IAuthTenantAdminService>();

                    var service = new SeedShopsOnStartup(context, authTenantAdmin);
                    await service.CreateShopsAndSeedStockAsync(SeedShopsOnStartup.SeedStockText);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}