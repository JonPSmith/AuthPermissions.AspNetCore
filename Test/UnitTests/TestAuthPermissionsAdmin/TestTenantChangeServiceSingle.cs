// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.SetupCode;
using Example3.InvoiceCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Test.StubClasses;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestTenantChangeServiceSingle
    {
        private readonly AuthPermissionsOptions _authOptionsSingle =
            new() { TenantType = TenantTypes.SingleLevel };

        private readonly ITestOutputHelper _output;

        public TestTenantChangeServiceSingle(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestAddSingleTenantAsyncOk()
        {
            //SETUP
            using var contexts = new SingleLevelTenantChangeSqlServerSetup(this);
            contexts.AuthPContext.SetupSingleTenantsInDb(contexts.InvoiceDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubInvoiceChangeServiceFactory(contexts.InvoiceDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingle, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.AddSingleTenantAsync("Tenant4");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.InvoiceDbContext.ChangeTracker.Clear();
            var companies = contexts.InvoiceDbContext.Companies.IgnoreQueryFilters().ToList();
            companies.Count.ShouldEqual(4);
            companies.Last().CompanyName.ShouldEqual("Tenant4");
        }

        [Fact]
        public async Task TestUpdateNameSingleTenantAsyncOk()
        {
            //SETUP
            using var contexts = new SingleLevelTenantChangeSqlServerSetup(this);
            var tenantIds = contexts.AuthPContext.SetupSingleTenantsInDb(contexts.InvoiceDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubInvoiceChangeServiceFactory(contexts.InvoiceDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingle, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.UpdateTenantNameAsync(tenantIds[1], "New Tenant");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            contexts.InvoiceDbContext.ChangeTracker.Clear();
            var companies = contexts.InvoiceDbContext.Companies.IgnoreQueryFilters().ToList();
            companies.Select(x => x.CompanyName).ShouldEqual(new[] { "Tenant1", "New Tenant", "Tenant3" });
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsync()
        {
            //SETUP
            using var contexts = new SingleLevelTenantChangeSqlServerSetup(this);
            var tenantIds = contexts.AuthPContext.SetupSingleTenantsInDb(contexts.InvoiceDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubInvoiceChangeServiceFactory(contexts.InvoiceDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingle, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(tenantIds[1]);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var companies = contexts.InvoiceDbContext.Companies.IgnoreQueryFilters().ToList();
            companies.Select(x => x.CompanyName).ShouldEqual(new[] { "Tenant1", "Tenant3" });
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsyncCheckReturn()
        {
            //SETUP
            using var contexts = new SingleLevelTenantChangeSqlServerSetup(this);
            var tenantIds = contexts.AuthPContext.SetupSingleTenantsInDb(contexts.InvoiceDbContext);
            contexts.AuthPContext.ChangeTracker.Clear();

            var changeServiceFactory = new StubInvoiceChangeServiceFactory(contexts.InvoiceDbContext);
            var service = new AuthTenantAdminService(contexts.AuthPContext,
                _authOptionsSingle, "en".SetupAuthPLoggingLocalizer(),
                changeServiceFactory, null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(tenantIds[1]);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var deletedId = ((InvoiceTenantChangeService)status.Result).DeletedTenantId;
            deletedId.ShouldEqual(tenantIds[1]);
        }

    }
}