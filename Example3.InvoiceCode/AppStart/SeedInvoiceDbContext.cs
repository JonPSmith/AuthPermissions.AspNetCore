// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.DataLayer.Classes;
using Example3.InvoiceCode.EfCoreClasses;
using Example3.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;

namespace Example3.InvoiceCode.AppStart
{
    public class SeedInvoiceDbContext
    {
        private readonly InvoicesDbContext _context;

        public SeedInvoiceDbContext(InvoicesDbContext context)
        {
            _context = context;
        }


        public async Task SeedInvoicesForAllTenantsAsync(IEnumerable<Tenant> authTenants)
        {

            foreach (var authTenant in authTenants)
            {
                var company = new CompanyTenant
                {
                    AuthPTenantId = authTenant.TenantId,
                    CompanyName = authTenant.TenantFullName,
                    DataKey = authTenant.GetTenantDataKey(),
                };
                _context.Add(company);

                for (int i = 0; i < 5; i++)
                {
                    var invoice = new Invoice
                    {
                        InvoiceName = $"{authTenant.TenantFullName}{i:D}",
                        DataKey = authTenant.GetTenantDataKey(),
                        DateCreated = DateTime.UtcNow,
                        LineItems = new List<LineItem>()
                    };
                    for (int j = 0; j < i + 1; j++)
                    {
                        invoice.LineItems.Add(new LineItem
                        {
                            ItemName = $"Item{j + 1}",
                            NumberItems = j + 1,
                            TotalPrice = 123,
                            DataKey = authTenant.GetTenantDataKey()
                        });
                    }

                    _context.Add(invoice);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}