// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using Example3.InvoiceCode.EfCoreClasses;
using Example3.InvoiceCode.EfCoreCode;

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
                var invoiceBuilder = new ExampleInvoiceBuilder(authTenant.GetTenantDataKey());

                for (int i = 0; i < 5; i++)
                {
                    var invoice = invoiceBuilder.CreateRandomInvoice(authTenant.TenantFullName);
                    _context.Add(invoice);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}