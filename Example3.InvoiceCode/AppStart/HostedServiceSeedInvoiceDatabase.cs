// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using Example3.InvoiceCode.EfCoreClasses;
using Example3.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Example3.InvoiceCode.AppStart
{
    /// <summary>
    /// If there are no RetailOutlets in the RetailDbContext it seeds the RetailDbContext with RetailOutlets and gives each of them some stock
    /// </summary>

    public class HostedServiceSeedInvoiceDatabase : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public HostedServiceSeedInvoiceDatabase(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                var context = services.GetRequiredService<InvoicesDbContext>();
                var numInvoices = await context.Invoices.IgnoreQueryFilters().CountAsync();
                if (numInvoices == 0)
                {
                    var authTenantAdmin = services.GetRequiredService<IAuthTenantAdminService>();

                    await SeedInvoicesForAllTenants(context, authTenantAdmin);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;


        //-----------------------------------------------------------
        // private methods

        private async Task SeedInvoicesForAllTenants(InvoicesDbContext context, IAuthTenantAdminService authTenantAdmin)
        {

            foreach (var authTenant in await authTenantAdmin.QueryTenants().ToArrayAsync())
            {
                for (int i = 0; i < 5; i++)
                {
                    var invoice = new Invoice
                    {
                        InvoiceName = $"{authTenant.TenantFullName}.{i:D}",
                        DataKey = authTenant.GetTenantDataKey(),
                        LineItems = new List<LineItem>()
                    };
                    for (int j = 0; j < i + 1; j++)
                    {
                        invoice.LineItems.Add(new LineItem
                        {
                            ItemName = $"Item{j+1}",
                            NumberItems = j+ 1,
                            TotalPrice = 123,
                            DataKey = authTenant.GetTenantDataKey()
                        });
                    }

                    context.Add(invoice);
                }
            }

            await context.SaveChangesAsync();
        }



    }
}