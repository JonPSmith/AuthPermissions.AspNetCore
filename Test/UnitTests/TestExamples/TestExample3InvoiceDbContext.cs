// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.DataLayer.EfCode;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.EfCoreClasses;
using Example3.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class TestExample3InvoiceDbContext
    {
        private readonly ITestOutputHelper _output;

        public TestExample3InvoiceDbContext(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestInvoicesDbContextInvoices()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<InvoicesDbContext>();
            using var context = new InvoicesDbContext(options, new StubGetDataKeyFilter(".1"));
            context.Database.EnsureCreated();

            //ATTEMPT
            var invoice1 = new Invoice
            {
                InvoiceName = "Test.1",
                DataKey = ".1"
            };
            var invoice2 = new Invoice
            {
                InvoiceName = "Test.2",
                DataKey = ".2"
            };
            context.AddRange(invoice1, invoice2);
            context.SaveChanges();

            //VERIFY
            context.ChangeTracker.Clear();
            context.Invoices.Count().ShouldEqual(1);
            context.Invoices.IgnoreQueryFilters().Count().ShouldEqual(2);
        }

        [Fact]
        public void TestInvoicesDbContextLineItems()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<InvoicesDbContext>();
            using var context = new InvoicesDbContext(options, new StubGetDataKeyFilter(".1"));
            context.Database.EnsureCreated();

            //ATTEMPT
            var invoice1 = new Invoice
            {
                InvoiceName = "Test.1",
                DataKey = ".1",
                LineItems = new List<LineItem>
                {
                    new LineItem { DataKey = ".1", NumberItems = 1, TotalPrice = 123 },
                    new LineItem { DataKey = ".1", NumberItems = 1, TotalPrice = 123 }
                }
            };
            var invoice2 = new Invoice
            {
                InvoiceName = "Test.2",
                DataKey = ".2",
                LineItems = new List<LineItem> { new LineItem { DataKey = ".2", NumberItems = 1, TotalPrice = 123 } }
            };
            context.AddRange(invoice1, invoice2);
            context.SaveChanges();

            //VERIFY
            context.ChangeTracker.Clear();
            context.Invoices.Include(x => x.LineItems).All(x => x.LineItems.Count == 2).ShouldBeTrue();
            context.LineItems.IgnoreQueryFilters().Count().ShouldEqual(3);
        }

        [Fact]
        public async Task TestInvoicesDbContextSeed()
        {
            //SETUP
            var options1 = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var authContext = new AuthPermissionsDbContext(options1);
            authContext.Database.EnsureCreated();
            authContext.SetupSingleTenantsInDb();

            var options2 = SqliteInMemory.CreateOptions<InvoicesDbContext>();
            using var invoiceContext = new InvoicesDbContext(options2, null);
            invoiceContext.Database.EnsureCreated();

            var seeder = new SeedInvoiceDbContext(invoiceContext);

            //ATTEMPT
            await seeder.SeedInvoicesForAllTenantsAsync(authContext.Tenants.ToArray());

            //VERIFY
            invoiceContext.ChangeTracker.Clear();
            invoiceContext.Companies.IgnoreQueryFilters().Select(x => x.CompanyName)
                .OrderBy(x => x).ToArray()
                .ShouldEqual(new []{ "Tenant1", "Tenant2", "Tenant3" });
            invoiceContext.Invoices.IgnoreQueryFilters().Count().ShouldEqual(5 * 3);
            invoiceContext.LineItems.IgnoreQueryFilters().Count().ShouldEqual(45);
        }


    }
}