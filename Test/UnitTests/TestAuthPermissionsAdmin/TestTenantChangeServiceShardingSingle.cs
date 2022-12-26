// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.SetupCode;
using Example6.SingleLevelSharding.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Test.StubClasses;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestTenantChangeServiceShardingSingle
    {
        private readonly AuthPermissionsOptions _authOptionsSingleSharding =
            new() { TenantType = TenantTypes.SingleLevel | TenantTypes.AddSharding };

        private readonly ITestOutputHelper _output;

        public TestTenantChangeServiceShardingSingle(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestAddSingleTenantAsyncToMainDatabaseOk()
        {
            //SETUP
            using var contexts = new ShardingSingleLevelTenantChangeSqlServerSetup(this);
            await contexts.AuthPContext.SetupSingleShardingTenantsInDbAsync(contexts.MainContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubChangeChangeServiceFactory(contexts.MainContext, this);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.AddSingleTenantAsync("Tenant4", null, false);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.MainContext.ChangeTracker.Clear();
            var companies = contexts.MainContext.Companies.IgnoreQueryFilters().ToList();
            companies.Count.ShouldEqual(4);
            companies.Last().CompanyName.ShouldEqual("Tenant4");
        }

        [Fact]
        public async Task TestAddSingleTenantAsyncToOtherDatabaseHasOwnDbOk()
        {
            //SETUP
            using var contexts = new ShardingSingleLevelTenantChangeSqlServerSetup(this);
            await contexts.AuthPContext.SetupSingleShardingTenantsInDbAsync(contexts.MainContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubChangeChangeServiceFactory(contexts.MainContext, this);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.AddSingleTenantAsync("Tenant4", null, true, "Other Database");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.MainContext.ChangeTracker.Clear();
            var mainCompanies = contexts.MainContext.Companies.IgnoreQueryFilters().ToList();
            mainCompanies.Count.ShouldEqual(3);
            contexts.OtherContext.DataKey.ShouldEqual(MultiTenantExtensions.DataKeyNoQueryFilter);
            var otherCompanies = contexts.OtherContext.Companies.ToList();
            otherCompanies.Single().CompanyName.ShouldEqual("Tenant4");
        }

        [Fact]
        public async Task TestAddSingleTenantAsyncHasOwnDbBad()
        {
            //SETUP
            using var contexts = new ShardingSingleLevelTenantChangeSqlServerSetup(this);
            await contexts.AuthPContext.SetupSingleShardingTenantsInDbAsync(contexts.MainContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubChangeChangeServiceFactory(contexts.MainContext, this);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.AddSingleTenantAsync("Tenant4", null, true);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors().ShouldEqual(
                "The hasOwnDb parameter is true, but the sharding database name 'Default Database' already has tenant(s) using that database.");
        }

        [Fact]
        public async Task TestUpdateNameSingleTenantAsyncOk()
        {
            //SETUP
            using var contexts = new ShardingSingleLevelTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.SetupSingleShardingTenantsInDbAsync(contexts.MainContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubChangeChangeServiceFactory(contexts.MainContext, this);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.UpdateTenantNameAsync(tenantIds[1], "New Tenant");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.MainContext.ChangeTracker.Clear();
            var companies = contexts.MainContext.Companies.IgnoreQueryFilters().ToList();
            companies.Select(x => x.CompanyName).ShouldEqual(new[] { "Tenant1", "New Tenant", "Tenant3" });
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsync()
        {
            //SETUP
            using var contexts = new ShardingSingleLevelTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.SetupSingleShardingTenantsInDbAsync(contexts.MainContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubChangeChangeServiceFactory(contexts.MainContext, this);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(tenantIds[1]);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var companies = contexts.MainContext.Companies.IgnoreQueryFilters().ToList();
            companies.Select(x => x.CompanyName).ShouldEqual(new[] { "Tenant1", "Tenant3" });
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsyncCheckReturn()
        {
            //SETUP
            using var contexts = new ShardingSingleLevelTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.SetupSingleShardingTenantsInDbAsync(contexts.MainContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubChangeChangeServiceFactory(contexts.MainContext, this);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(tenantIds[1]);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var deletedId = ((ShardingTenantChangeService)status.Result).DeletedTenantId;
            deletedId.ShouldEqual(tenantIds[1]);
        }

        [Fact]
        public async Task TestMoveToDifferentDatabaseAsync()
        {
            //SETUP
            using var contexts = new ShardingSingleLevelTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.SetupSingleShardingTenantsInDbAsync(contexts.MainContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubChangeChangeServiceFactory(contexts.MainContext, this);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.MoveToDifferentDatabaseAsync(tenantIds[1], true, "Other Database");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.MainContext.ChangeTracker.Clear();
            var mainCompanies = contexts.MainContext.Companies.IgnoreQueryFilters().ToList();
            mainCompanies.Count.ShouldEqual(2);
            contexts.OtherContext.DataKey.ShouldEqual(MultiTenantExtensions.DataKeyNoQueryFilter);
            var query = contexts.OtherContext.Companies;
            _output.WriteLine(query.ToQueryString());
            var otherCompanies = query.ToList();
            otherCompanies.Single().CompanyName.ShouldEqual("Tenant2");
        }

        [Fact]
        public async Task TestMoveToDifferentDatabaseAsyncJustChangeHasOwnDb()
        {
            //SETUP
            using var contexts = new ShardingSingleLevelTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.SetupSingleShardingTenantsInDbAsync(contexts.MainContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubChangeChangeServiceFactory(contexts.MainContext, this);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            var preStatus = await service.AddSingleTenantAsync("Tenant4", null, true, "Other Database");
            preStatus.IsValid.ShouldBeTrue(preStatus.GetAllErrors());
            var tenant4Id = contexts.AuthPContext.Tenants.Single(x => x.TenantFullName == "Tenant4").TenantId;
            contexts.AuthPContext.ChangeTracker.Clear();

            //ATTEMPT
            var status = await service.MoveToDifferentDatabaseAsync(tenant4Id, false, "Other Database");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.AuthPContext.ChangeTracker.Clear();
            var tenant4 = contexts.AuthPContext.Tenants.Single(x => x.TenantFullName == "Tenant4");
            tenant4.HasOwnDb.ShouldBeFalse();
            status.Message.ShouldEqual("The tenant wasn't moved but its HasOwnDb was changed to False.");
        }

    }
}