// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using Example4.ShopCode.EfCoreClasses;
using Example4.ShopCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Test.TestHelpers;
using TestSupport.Attributes;
using TestSupport.EfHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class TestExample4RetailDbContext
    {
        private readonly ITestOutputHelper _output;

        public TestExample4RetailDbContext(ITestOutputHelper output)
        {
            _output = output;
        }

        [RunnableInDebugOnly]
        public void TestRetailDbContextMigrate()
        {
            //SETUP
            //var options = SqliteInMemory.CreateOptions<RetailDbContext>();
            var optionsBuilder = new DbContextOptionsBuilder<RetailDbContext>();
            optionsBuilder.UseSqlServer(this.GetUniqueDatabaseConnectionString("XX"),
            x => x.MigrationsHistoryTable("NotTheNormalName"));

            using var context = new RetailDbContext(optionsBuilder.Options, new StubGetDataKeyFilter("."));
            context.Database.EnsureClean(false);

            //ATTEMPT
            context.Database.Migrate();

            //VERIFY
            context.ChangeTracker.Clear();
            context.RetailOutlets.Count().ShouldEqual(0);
        }

        [Theory]
        [InlineData(".1.2", 1)]
        [InlineData(".2.3", 0)]
        public void TestRetailDbContextShopStockNotFilteredByQueryFilter(string retailKey, int numFound)
        {
            //SETUP
            //var options = SqliteInMemory.CreateOptions<RetailDbContext>();
            var options = this.CreateUniqueClassOptions<RetailDbContext>();
            using var context = new RetailDbContext(options, new StubGetDataKeyFilter(retailKey));
            context.Database.EnsureClean();

            //ATTEMPT
            var retailOutlet = new RetailOutlet(new StubTenantParts("SanFran | Dress4U", ".1.2"));
            context.Add(new ShopStock("white dress", 123, 5, retailOutlet));
            context.SaveChanges();

            //VERIFY
            context.ChangeTracker.Clear();
            context.RetailOutlets.Count().ShouldEqual(numFound);
            context.ShopStocks.Count().ShouldEqual(numFound);
        }

        [Theory]
        [InlineData(".1.2", true)]
        [InlineData(".2.3", false)]
        public void TestRetailDbContextShopSaleFilteredByQueryFilter(string retailKey, bool saleOk)
        {
            //SETUP
            //var options = SqliteInMemory.CreateOptions<RetailDbContext>();
            var options = this.CreateUniqueClassOptions<RetailDbContext>();
            using var context = new RetailDbContext(options, new StubGetDataKeyFilter(retailKey));
            context.Database.EnsureClean();

            var retailOutlet = new RetailOutlet(new StubTenantParts("SanFran | Dress4U", ".1.2"));
            var stock = new ShopStock("white dress", 123, 5, retailOutlet);
            context.Add(stock);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            //ATTEMPT
            var foundStock = context.ShopStocks.SingleOrDefault(x => x.StockName == "white dress");
            var status = ShopSale.CreateSellAndUpdateStock(1, foundStock, "white dress");
            if (status.IsValid)
            {
                context.Add(status.Result);
                context.SaveChanges();
            }

            //VERIFY
            _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
            status.IsValid.ShouldEqual(saleOk);
        }

        [Theory]
        [InlineData(1, true)]
        [InlineData(6, false)]
        public void TestShopSaleCreateSellAndUpdateStock(int buyNum, bool saleOk)
        {
            //SETUP
            //var options = SqliteInMemory.CreateOptions<RetailDbContext>();
            var options = this.CreateUniqueClassOptions<RetailDbContext>();
            using var context = new RetailDbContext(options, new StubGetDataKeyFilter(".1.2"));
            context.Database.EnsureClean();

            var retailOutlet = new RetailOutlet(new StubTenantParts("SanFran | Dress4U", ".1.2"));
            var stock = new ShopStock("white dress", 123, 5, retailOutlet);
            context.Add(stock);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            //ATTEMPT
            var foundStock = context.ShopStocks.SingleOrDefault(x => x.StockName == "white dress");
            var status = ShopSale.CreateSellAndUpdateStock(buyNum, foundStock, "white dress");
            if (status.IsValid)
            {
                context.Add(status.Result);
                context.SaveChanges();
            }

            //VERIFY
            _output.WriteLine(status.IsValid ? status.Message : status.GetAllErrors());
            status.IsValid.ShouldEqual(saleOk);

            context.ChangeTracker.Clear();
            var rStock = context.ShopStocks.Single();
            rStock.NumInStock.ShouldEqual(saleOk ? 4 : 5);
            var rSale = context.ShopSales.SingleOrDefault();
            (rSale != null).ShouldEqual(saleOk);
        }
    }
}