// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using EntityFramework.Exceptions.SqlServer;
using Example4.ShopCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestTenantAdminServicesHierarchical
    {
        private readonly ITestOutputHelper _output;

        public TestTenantAdminServicesHierarchical(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestQueryEndLeafTenantsHierarchical()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant }, null, null);

            //ATTEMPT
            var tenants = service.QueryEndLeafTenants().ToList();

            //VERIFY
            tenants.Count.ShouldEqual(4);
            tenants.Select(x => x.GetTenantName()).OrderBy(x => x).ToArray()
                .ShouldEqual(new[] { "Shop1", "Shop2", "Shop3", "Shop4" });
        }

        [Fact]
        public async Task TestGetHierarchicalTenantChildrenViaIdAsync()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenantIds = await context.SetupHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant }, null, null);

            //ATTEMPT
            var children = await service.GetHierarchicalTenantChildrenViaIdAsync(tenantIds[1]);

            //VERIFY
            children.Count.ShouldEqual(3);
            children.Select(x => x.GetTenantName()).OrderBy(x => x).ToArray()
                .ShouldEqual(new[] { "SanFran", "Shop1", "Shop2" });
        }


        [Fact]
        public async Task TestAddHierarchicalTenantAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenantIds = await context.SetupHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant }, null, null);

            //ATTEMPT
            var status = await service.AddHierarchicalTenantAsync("LA", tenantIds[1]);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            var newTenant = tenants.SingleOrDefault(x => x.TenantFullName == "Company | West Coast | LA");
            tenants.Count.ShouldEqual(10);
            newTenant.ShouldNotBeNull();
            newTenant.GetTenantDataKey().ShouldEqual(".1.2.10");
        }

        [Fact]
        public async Task TestAddHierarchicalTenantAsyncTopeLevelOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenantIds = await context.SetupHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant }, null, null);

            //ATTEMPT
            var status = await service.AddHierarchicalTenantAsync("New Company", 0);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            var newTenant = tenants.SingleOrDefault(x => x.TenantFullName == "New Company");
            tenants.Count.ShouldEqual(10);
            newTenant.ShouldNotBeNull();
            newTenant.GetTenantDataKey().ShouldEqual(".10");
        }

        [Fact]
        public async Task TestAddHierarchicalTenantAsyncDuplicate()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            var tenantIds = await context.SetupHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant }, null, null);

            //ATTEMPT
            var status = await service.AddHierarchicalTenantAsync("West Coast", tenantIds[0]);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("There is already a Tenant with a value: name = Company | West Coast");
        }

        [Fact]
        public async Task TestUpdateHierarchicalTenantAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                    builder.UseSqlite(context.Database.GetDbConnection()));
                appOptions.TurnOffDispose();
                var retailContext = new RetailDbContext(appOptions, null);

                var tenantIds = await context.SetupHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubITenantChangeServiceFactory(retailContext);
                var service = new AuthTenantAdminService(context,
                    new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                    subTenantChangeService, null);

                //ATTEMPT
                var status = await service.UpdateTenantNameAsync(tenantIds[1], "West Area");

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                var tenants = context.Tenants.ToList();
                foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
                {
                    _output.WriteLine(tenant.ToString());
                }

                tenants.Count(x => x.TenantFullName.StartsWith("Company | West Area")).ShouldEqual(4);
            }
        }

        [Fact]
        public async Task TestUpdateHierarchicalTenantSqlServerOk()
        {
            //SETUP
            using var contexts = new TenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.SetupHierarchicalTenantInDbAsync();
            contexts.RetailDbContext.SetupHierarchicalRetailAndStock(contexts.AuthPContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(contexts.AuthPContext, new AuthPermissionsOptions
            {
                TenantType = TenantTypes.HierarchicalTenant,
                AppConnectionString = contexts.ConnectionString
            }, new StubRetailTenantChangeServiceFactory(), null);

            //ATTEMPT
            var status = await service.UpdateTenantNameAsync(tenantIds[1], "West Area");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.AuthPContext.ChangeTracker.Clear();
            var tenants = contexts.AuthPContext.Tenants.ToList();
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.Count(x => x.TenantFullName.StartsWith("Company | West Area")).ShouldEqual(4);
            contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters()
                .Count(x => x.FullName.StartsWith("Company | West Area")).ShouldEqual(2);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncBaseOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                    builder.UseSqlite(context.Database.GetDbConnection()));
                appOptions.TurnOffDispose();
                var retailContext = new RetailDbContext(appOptions, null);

                await context.SetupHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubITenantChangeServiceFactory(retailContext);
                var service = new AuthTenantAdminService(context,
                    new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                    subTenantChangeService, null);

                //ATTEMPT
                var status = await service.MoveHierarchicalTenantToAnotherParentAsync(7, 4);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Message.ShouldEqual("Successfully moved the tenant originally named 'Company | West Coast | SanFran | Shop1' to the new named 'Company | East Coast | New York | Shop1'.");
                subTenantChangeService.MoveReturnedTuples
                    .ShouldEqual(new List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)>
                {
                    (".1.2.5.7", ".1.3.4.7", 7, "Company | East Coast | New York | Shop1")
                });
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.ChangeTracker.Clear();
                var tenants = context.Tenants.ToList();
                foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
                {
                    _output.WriteLine(tenant.ToString());
                }
                tenants.Count(x => x.TenantFullName.StartsWith("Company | East Coast | New York | Shop1")).ShouldEqual(1);
            }
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                    builder.UseSqlite(context.Database.GetDbConnection()));
                appOptions.TurnOffDispose();
                var retailContext = new RetailDbContext(appOptions, null);

                await context.SetupHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubITenantChangeServiceFactory(retailContext);
                var service = new AuthTenantAdminService(context,
                    new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                    subTenantChangeService, null);

                //ATTEMPT
                var status = await service.MoveHierarchicalTenantToAnotherParentAsync(2, 3);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Message.ShouldEqual("Successfully moved the tenant originally named 'Company | West Coast' to the new named 'Company | East Coast | West Coast'.");
                subTenantChangeService.MoveReturnedTuples
                    .ShouldEqual(new List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)>
                {
                    (".1.2", ".1.3.2", 2, "Company | East Coast | West Coast"),
                    (".1.2.5", ".1.3.2.5", 5, "Company | East Coast | West Coast | SanFran"),
                    (".1.2.5.6", ".1.3.2.5.6", 6, "Company | East Coast | West Coast | SanFran | Shop2"),
                    (".1.2.5.7", ".1.3.2.5.7", 7, "Company | East Coast | West Coast | SanFran | Shop1")
                });
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.ChangeTracker.Clear();
                var tenants = context.Tenants.ToList();
                foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
                {
                    _output.WriteLine(tenant.ToString());
                }
                tenants.Count(x => x.TenantFullName.StartsWith("Company | East Coast | West Coast")).ShouldEqual(4);
            }
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncSqlServerOk()
        {
            //SETUP
            using var contexts = new TenantChangeSqlServerSetup(this);
            await contexts.AuthPContext.SetupHierarchicalTenantInDbAsync();
            contexts.RetailDbContext.SetupHierarchicalRetailAndStock(contexts.AuthPContext);
            var preStocks = contexts.RetailDbContext.ShopStocks.IgnoreQueryFilters()
                .Include(x => x.Shop).ToList();
            foreach (var tenant in preStocks.OrderBy(x => x.DataKey))
            {
                _output.WriteLine($"{tenant.Shop.ShortName}: DataKey = {tenant.DataKey}");
            }
            contexts.AuthPContext.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(contexts.AuthPContext, new AuthPermissionsOptions
            {
                TenantType = TenantTypes.HierarchicalTenant,
                AppConnectionString = contexts.ConnectionString
            }, new StubRetailTenantChangeServiceFactory(), null);

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(2, 3);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.AuthPContext.ChangeTracker.Clear();
            contexts.RetailDbContext.ChangeTracker.Clear();

            var authTenants = contexts.AuthPContext.Tenants.ToList();
            foreach (var tenant in authTenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            var shopStocks = contexts.RetailDbContext.ShopStocks.IgnoreQueryFilters()
                .Include(x => x.Shop).ToList();
            foreach (var tenant in shopStocks.OrderBy(x => x.DataKey))
            {
                _output.WriteLine($"{tenant.Shop.ShortName}: DataKey = {tenant.DataKey}");
            }
            authTenants.Count(x => x.TenantFullName.StartsWith("Company | East Coast | West Coast")).ShouldEqual(4);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncMoveToTop()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                    builder.UseSqlite(context.Database.GetDbConnection()));
                appOptions.TurnOffDispose();
                var retailContext = new RetailDbContext(appOptions, null);

                await context.SetupHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubITenantChangeServiceFactory(retailContext);
                var service = new AuthTenantAdminService(context,
                    new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                    subTenantChangeService, null);

                //ATTEMPT
                var status = await service.MoveHierarchicalTenantToAnotherParentAsync(3, 0);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Message.ShouldEqual("Successfully moved the tenant originally named 'Company | East Coast' to top level.");
                subTenantChangeService.MoveReturnedTuples
                    .ShouldEqual(new List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)>
                {
                    (".1.3", ".3", 3, "East Coast"),
                    (".1.3.4", ".3.4", 4, "East Coast | New York"),
                    (".1.3.4.8", ".3.4.8", 8, "East Coast | New York | Shop3"),
                    (".1.3.4.9", ".3.4.9", 9, "East Coast | New York | Shop4")
                });
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.ChangeTracker.Clear();
                var tenants = context.Tenants.ToList();
                foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
                {
                    _output.WriteLine(tenant.ToString());
                }
                tenants.Count(x => x.TenantFullName.StartsWith("East Coast")).ShouldEqual(4);
            }
        }


        [Theory]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncMoveToChild(int parentTenantId)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using var context = new AuthPermissionsDbContext(options);

            context.Database.EnsureCreated();

            var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                builder.UseSqlite(context.Database.GetDbConnection()));
            appOptions.TurnOffDispose();
            var retailContext = new RetailDbContext(appOptions, null);

            await context.SetupHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var subTenantChangeService = new StubITenantChangeServiceFactory(retailContext);
            var service = new AuthTenantAdminService(context,
                new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                subTenantChangeService, null);

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(2, parentTenantId);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("You cannot move a tenant one of its children.");
        }


        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncMoveToSelf()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using var context = new AuthPermissionsDbContext(options);

            context.Database.EnsureCreated();

            var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                builder.UseSqlite(context.Database.GetDbConnection()));
            appOptions.TurnOffDispose();
            var retailContext = new RetailDbContext(appOptions, null);

            await context.SetupHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var subTenantChangeService = new StubITenantChangeServiceFactory(retailContext);
            var service = new AuthTenantAdminService(context,
                new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                subTenantChangeService, null);

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(2, 2);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("You cannot move a tenant to itself.");
        }

        [Fact]
        public async Task TestDeleteTenantAsyncBaseOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            int numTenants;
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                    builder.UseSqlite(context.Database.GetDbConnection()));
                appOptions.TurnOffDispose();
                var retailContext = new RetailDbContext(appOptions, null);

                numTenants = (await context.SetupHierarchicalTenantInDbAsync()).Count;
                context.ChangeTracker.Clear();

                var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                    new StubITenantChangeServiceFactory(retailContext), null);

                //ATTEMPT
                var status = await service.DeleteTenantAsync(7);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                var deleteLogs = ((StubITenantChangeServiceFactory.StubITenantChangeService)status.Result).DeleteReturnedTuples;
                deleteLogs.ShouldEqual(new List<(string fullTenantName, string dataKey)>
                {
                    ("Company | West Coast | SanFran | Shop1", ".1.2.5.7")
                });
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.ChangeTracker.Clear();
                var tenants = context.Tenants.ToList();
                foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
                {
                    _output.WriteLine(tenant.ToString());
                }
                tenants.SingleOrDefault(x => x.TenantFullName == "Company | West Coast | SanFran | Shop1").ShouldBeNull();
                tenants.Count.ShouldEqual(numTenants - 1);
            }
        }

        [Fact]
        public async Task TestDeleteTenantAsyncAnotherParentOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                    builder.UseSqlite(context.Database.GetDbConnection()));
                appOptions.TurnOffDispose();
                var retailContext = new RetailDbContext(appOptions, null);

                var tenantIds = await context.SetupHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant},
                    new StubITenantChangeServiceFactory(retailContext), null);
                options.StopNextDispose();

                //ATTEMPT
                var status = await service.DeleteTenantAsync(2);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                var deleteLogs = ((StubITenantChangeServiceFactory.StubITenantChangeService)status.Result).DeleteReturnedTuples;
                deleteLogs.ShouldEqual(new List<(string fullTenantName, string dataKey)>
                {
                    ("Company | West Coast | SanFran | Shop2", ".1.2.5.6"),
                    ("Company | West Coast | SanFran | Shop1", ".1.2.5.7"),
                    ("Company | West Coast | SanFran", ".1.2.5"),
                    ("Company | West Coast", ".1.2"),
                });
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.ChangeTracker.Clear();
                var tenants = context.Tenants.ToList();
                foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
                {
                    _output.WriteLine(tenant.ToString());
                }
                tenants.Count(x => x.TenantFullName.StartsWith("Company | West Coast")).ShouldEqual(0);
                tenants.Count.ShouldEqual(5);
            }
        }

        [Fact]
        public async Task TestDeleteTenantAsyncUserOnActualTenantBad()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                builder.UseSqlite(context.Database.GetDbConnection()));
            appOptions.TurnOffDispose();
            var retailContext = new RetailDbContext(appOptions, null);

            await context.SetupHierarchicalTenantInDbAsync();
            var tenantToDelete = context.Find<Tenant>(7);
            context.Add(new AuthUser("123", "me@gmail.com", "Mr Me", new List<RoleToPermissions>(), tenantToDelete));
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                new StubITenantChangeServiceFactory(retailContext), null);
            options.StopNextDispose();

            //ATTEMPT
            var status = await service.DeleteTenantAsync(tenantToDelete.TenantId);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors().ShouldEqual("This delete is aborted because this tenant is linked to the user 'Mr Me'.");
        }

        [Fact]
        public async Task TestDeleteTenantAsyncUserOnChildTenant()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.StopNextDispose();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                builder.UseSqlite(context.Database.GetDbConnection()));
            appOptions.TurnOffDispose();
            var retailContext = new RetailDbContext(appOptions, null);

            await context.SetupHierarchicalTenantInDbAsync();
            var childTenant = context.Find<Tenant>(7);
            context.Add(new AuthUser("123", "me@gmail.com", "Mr Me", new List<RoleToPermissions>(), childTenant));
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant },
                new StubITenantChangeServiceFactory(retailContext), null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(2);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors().ShouldEqual("This delete is aborted because this tenant or its children tenants are linked to the user 'Mr Me'.");
        }
    }
}