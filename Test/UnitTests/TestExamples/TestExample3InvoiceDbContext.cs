// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
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
        public void TestRetailDbContextInvoices()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<InvoicesDbContext>();
            using var context = new InvoicesDbContext(options, new StubGetDataKeyFilter(".1"));
            context.Database.EnsureCreated();

            //ATTEMPT
            var invoice1 = new Invoice
            {
                DataKey = ".1"
            };
            var invoice2 = new Invoice
            {
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
        public void TestRetailDbContextLineItems()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<InvoicesDbContext>();
            using var context = new InvoicesDbContext(options, new StubGetDataKeyFilter(".1"));
            context.Database.EnsureCreated();

            //ATTEMPT
            var invoice1 = new Invoice
            {
                DataKey = ".1",
                LineItems = new List<LineItem>
                {
                    new LineItem { DataKey = ".1", NumberItems = 1, TotalPrice = 123 },
                    new LineItem { DataKey = ".1", NumberItems = 1, TotalPrice = 123 }
                }
            };
            var invoice2 = new Invoice
            {
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


    }
}