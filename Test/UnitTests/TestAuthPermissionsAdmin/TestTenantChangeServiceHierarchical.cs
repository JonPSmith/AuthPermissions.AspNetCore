// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.SetupCode;
using Example4.ShopCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Test.StubClasses;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestTenantChangeServiceHierarchical
    {

        private readonly AuthPermissionsOptions _authOptionsHierarchical =
            new() { TenantType = TenantTypes.HierarchicalTenant };

        private readonly ITestOutputHelper _output;

        public TestTenantChangeServiceHierarchical(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestSetupHierarchicalTenantInDbAsync()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);

            //ATTEMPT
            await contexts.AuthPContext.BulkLoadHierarchicalTenantInDbAsync(contexts.RetailDbContext);

            //VERIFY
            contexts.AuthPContext.ChangeTracker.Clear();
            var tenants = contexts.AuthPContext.Tenants.OrderBy(x => x.TenantId).ToList();
            foreach (var tenant in tenants)
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.Select(x => x.TenantFullName).ToArray().ShouldEqual(new []
            {
                "Company",
                "Company | West Coast",
                "Company | East Coast",
                "Company | West Coast | SanFran",
                "Company | East Coast | New York",
                "Company | West Coast | SanFran | Shop1",
                "Company | West Coast | SanFran | Shop2",
                "Company | East Coast | New York | Shop3",
                "Company | East Coast | New York | Shop4"
            });
        }


        [Fact]
        public async Task TestAddHierarchicalTenantAsyncOk()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.BulkLoadHierarchicalTenantInDbAsync(contexts.RetailDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubRetailChangeServiceFactory(contexts.RetailDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsHierarchical, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.AddHierarchicalTenantAsync("LA", tenantIds[1]);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.RetailDbContext.ChangeTracker.Clear();
            var retails = contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters().ToList();
            retails.Count.ShouldEqual(10);

            var newTenant = retails.SingleOrDefault(x => x.FullName == "Company | West Coast | LA");
            newTenant.ShouldNotBeNull();
            newTenant.DataKey.ShouldEqual("1.2.10.");
        }

        [Fact]
        public async Task TestUpdateHierarchicalTenantAsyncOk()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.BulkLoadHierarchicalTenantInDbAsync(contexts.RetailDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubRetailChangeServiceFactory(contexts.RetailDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsHierarchical, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.UpdateTenantNameAsync(tenantIds[1], "West Area");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.RetailDbContext.ChangeTracker.Clear();
            var retails = contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters().ToList();
            retails.Count(x => x.FullName.StartsWith("Company | West Area")).ShouldEqual(4);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncBaseOk()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.BulkLoadHierarchicalTenantInDbAsync(contexts.RetailDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubRetailChangeServiceFactory(contexts.RetailDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsHierarchical, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(6, 5);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.RetailDbContext.ChangeTracker.Clear();
            var retails = contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters().ToList();
            foreach (var tenant in retails.OrderBy(x => x.DataKey))
            {
                _output.WriteLine(tenant.FullName);
            }

            retails.Count(x => x.FullName.StartsWith("Company | East Coast | New York | Shop1"))
                    .ShouldEqual(1);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncOk()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.BulkLoadHierarchicalTenantInDbAsync(contexts.RetailDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubRetailChangeServiceFactory(contexts.RetailDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsHierarchical, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(2, 3);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Message.ShouldEqual(
                "Successfully moved the tenant originally named 'Company | West Coast' to the new named 'Company | East Coast | West Coast'.");

            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.RetailDbContext.ChangeTracker.Clear();
            var retails = contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters().ToList();
            foreach (var tenant in retails.OrderBy(x => x.DataKey))
            {
                _output.WriteLine(tenant.FullName);
            }

            retails.Count(x => x.FullName.StartsWith("Company | East Coast | West Coast")).ShouldEqual(4);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncMoveToTop()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.BulkLoadHierarchicalTenantInDbAsync(contexts.RetailDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubRetailChangeServiceFactory(contexts.RetailDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsHierarchical, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(3, 0);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Message.ShouldEqual(
                "Successfully moved the tenant originally named 'Company | East Coast' to top level.");

            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.RetailDbContext.ChangeTracker.Clear();
            var retails = contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters().ToList();
            foreach (var tenant in retails.OrderBy(x => x.DataKey))
            {
                _output.WriteLine(tenant.FullName);
            }

            retails.Count(x => x.FullName.StartsWith("East Coast")).ShouldEqual(4);
        }

        [Fact]
        public async Task TestDeleteTenantAsyncBaseOk()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.BulkLoadHierarchicalTenantInDbAsync(contexts.RetailDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubRetailChangeServiceFactory(contexts.RetailDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsHierarchical, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(6);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());

            status.IsValid.ShouldBeTrue(status.GetAllErrors());

            contexts.RetailDbContext.ChangeTracker.Clear();
            var retails = contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters().ToList();
            foreach (var tenant in retails.OrderBy(x => x.DataKey))
            {
                _output.WriteLine(tenant.FullName);
            }

            retails.SingleOrDefault(x => x.FullName == "Company | West Coast | SanFran | Shop1").ShouldBeNull();
            retails.Count.ShouldEqual(tenantIds.Count - 1);
        }

        [Fact]
        public async Task TestDeleteTenantAsyncAnotherParentOk()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.BulkLoadHierarchicalTenantInDbAsync(contexts.RetailDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubRetailChangeServiceFactory(contexts.RetailDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsHierarchical, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(2);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.RetailDbContext.ChangeTracker.Clear();
            var retails = contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters().ToList();
            foreach (var tenant in retails.OrderBy(x => x.DataKey))
            {
                _output.WriteLine(tenant.FullName);
            }

            retails.SingleOrDefault(x => x.FullName == "Company | West Coast").ShouldBeNull();
            retails.Count.ShouldEqual(5);

            var deletedIds = ((RetailTenantChangeService)status.Result).DeletedTenantIds;
            deletedIds.ShouldEqual(new List<int>{ 6, 7, 4, 2 });
        }
    }
}