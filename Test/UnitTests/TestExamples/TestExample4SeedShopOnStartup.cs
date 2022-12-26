// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SetupCode;
using Example4.ShopCode.AppStart;
using Example4.ShopCode.EfCoreCode;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class TestExample4SeedShopOnStartup
    {
        private readonly ITestOutputHelper _output;

        public TestExample4SeedShopOnStartup(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestCreateShopsAndSeedStockAsyncSimpleStock()
        {
            //SETUP
            var authPOptions = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var authPContext = new AuthPermissionsDbContext(authPOptions);
            authPContext.Database.EnsureCreated();
            await authPContext.BulkLoadHierarchicalTenantInDbAsync();
            var tenantService = new AuthTenantAdminService(authPContext, 
                new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant}, 
                "en".SetupAuthPLoggingLocalizer(),
                null, null);

            var rOptions = SqliteInMemory.CreateOptions<RetailDbContext>();
            //var rOptions = this.CreateUniqueClassOptions<RetailDbContext>();
            using var context = new RetailDbContext(rOptions, new StubGetDataKeyFilter(""));
            context.Database.EnsureCreated();

            var service = new SeedShopsOnStartup(context, tenantService);

            //ATTEMPT
            await service.CreateShopsAndSeedStockAsync("Shop1: Flower dress|50, Tiny dress|22");

            //VERIFY
            context.RetailOutlets.Select(x => x.ShortName).ToList()
                .OrderBy(x => x).ToArray()
                .ShouldEqual(new[] { "Shop1", "Shop2", "Shop3", "Shop4" });
        }

        [Fact]
        public async Task TestCreateShopsAndSeedStockAsyncAllStock()
        {
            //SETUP
            var authPOptions = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var authPContext = new AuthPermissionsDbContext(authPOptions);
            authPContext.Database.EnsureCreated();
            await authPContext.BulkLoadHierarchicalTenantInDbAsync();
            var tenantService = new AuthTenantAdminService(authPContext,
                new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                "en".SetupAuthPLoggingLocalizer(),
                null, null);

            var rOptions = SqliteInMemory.CreateOptions<RetailDbContext>();
            //var rOptions = this.CreateUniqueClassOptions<RetailDbContext>();
            using var context = new RetailDbContext(rOptions, new StubGetDataKeyFilter(""));
            context.Database.EnsureCreated();

            var service = new SeedShopsOnStartup(context, tenantService);

            //ATTEMPT
            await service.CreateShopsAndSeedStockAsync(@"Shop1: Flower dress|50, Tiny dress|22
Shop2: Blue tie|15, Red tie|20
Shop3: White shirt|40, Blue shirt|30
Shop4: Cat food (large)|40, Cat food (small)|10");

            //VERIFY
            var stock = context.ShopStocks.ToList();
            foreach (var shopStock in stock)
            {
                _output.WriteLine(shopStock.ToString());
            }
            stock.Count.ShouldEqual(8);

        }
    }
}