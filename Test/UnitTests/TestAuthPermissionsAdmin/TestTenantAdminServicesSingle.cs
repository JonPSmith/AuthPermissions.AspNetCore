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
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestTenantAdminServicesSingle
    {
        private readonly ITestOutputHelper _output;

        public TestTenantAdminServicesSingle(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestQueryTenantsSingle()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions{TenantType = TenantTypes.SingleLevel}, null, null);

            //ATTEMPT
            var tenants = service.QueryTenants().ToList();

            //VERIFY
            tenants.Count.ShouldEqual(3);
            tenants.Select(x => x.TenantFullName).ShouldEqual(new[]{ "Tenant1", "Tenant2", "Tenant3" });
        }

        [Fact]
        public void TestQueryEndLeafTenantsSingle()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions{TenantType = TenantTypes.SingleLevel}, null, null);

            //ATTEMPT
            var tenants = service.QueryEndLeafTenants().ToList();

            //VERIFY
            tenants.Count.ShouldEqual(3);
            tenants.Select(x => x.TenantFullName).ShouldEqual(new[] { "Tenant1", "Tenant2", "Tenant3" });
        }


        [Fact]
        public async Task TestAddSingleTenantAsyncOk()
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

                var tenantIds = context.SetupSingleTenantsInDb();
                context.ChangeTracker.Clear();

                var tenantChange = new StubITenantChangeServiceFactory(retailContext);
                var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel },
                    tenantChange, null);

                //ATTEMPT
                var status = await service.AddSingleTenantAsync("Tenant4");

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                tenantChange.NewTenantName.ShouldEqual( "Tenant4" );
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                var tenants = context.Tenants.ToList();
                tenants.Count.ShouldEqual(4);
                tenants.Last().TenantFullName.ShouldEqual("Tenant4");
            }
        }

        [Fact]
        public async Task TestAddSingleTenantAsyncDuplicate()
        {
            //SETUP
            using var contexts = new TenantChangeSqlServerSetup(this);
            var tenantIds = contexts.AuthPContext.SetupSingleTenantsInDb();
            contexts.RetailDbContext.SetupSingleRetailAndStock();
            contexts.AuthPContext.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(contexts.AuthPContext, new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel,
                AppConnectionString = contexts.ConnectionString
            }, new StubRetailTenantChangeServiceFactory(), null);

            //ATTEMPT
            var status = await service.AddSingleTenantAsync("Tenant2");

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("There is already a Tenant with a value: name = Tenant2");
        }

        [Fact]
        public async Task TestUpdateSingleTenantAsyncOk()
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

                var tenantIds = context.SetupSingleTenantsInDb();
                context.ChangeTracker.Clear();

                var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel },
                    new StubITenantChangeServiceFactory(retailContext), null);

                //ATTEMPT
                var status = await service.UpdateTenantNameAsync(tenantIds[1], "New Tenant");

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                var tenants = context.Tenants.ToList();
                tenants.Count.ShouldEqual(3);
                tenants.Select(x => x.TenantFullName).ShouldEqual(new[] { "Tenant1", "New Tenant", "Tenant3" });
            }
        }

        [Fact]
        public async Task TestUpdateSingleTenantAsyncSqlServerOk()
        {
            //SETUP
            using var contexts = new TenantChangeSqlServerSetup(this);
            var tenantIds = contexts.AuthPContext.SetupSingleTenantsInDb();
            contexts.RetailDbContext.SetupSingleRetailAndStock();
            contexts.AuthPContext.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(contexts.AuthPContext, new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel,
                AppConnectionString = contexts.ConnectionString
            }, new StubRetailTenantChangeServiceFactory(), null);

            //ATTEMPT
            var status = await service.UpdateTenantNameAsync(tenantIds[1], "New Tenant");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = contexts.AuthPContext.Tenants.ToList();
            tenants.Select(x => x.TenantFullName).ShouldEqual(new[] { "Tenant1", "New Tenant", "Tenant3" });
            contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters().Select(x => x.FullName)
                .ToArray().ShouldEqual(new[] { "Tenant1", "New Tenant", "Tenant3" });
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsyncSqliteOk()
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

                var tenantIds = context.SetupSingleTenantsInDb();
                context.ChangeTracker.Clear();

                var service = new AuthTenantAdminService(context, new AuthPermissionsOptions{TenantType = TenantTypes.SingleLevel},
                    new StubITenantChangeServiceFactory(retailContext), null);

                //ATTEMPT
                var status = await service.DeleteTenantAsync(tenantIds[1]);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                var deleteLogs = ((StubITenantChangeServiceFactory.StubITenantChangeService)status.Result).DeleteReturnedTuples; 
                deleteLogs.ShouldEqual(new List<(string fullTenantName, string dataKey)>
                {
                    ("Tenant2", "2.")
                });
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                var tenants = context.Tenants.ToList();
                tenants.Select(x => x.TenantFullName).ShouldEqual(new[] { "Tenant1", "Tenant3" });
            }
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsyncSqlServerOk()
        {
            //SETUP
            using var contexts = new TenantChangeSqlServerSetup(this);
            var tenantIds = contexts.AuthPContext.SetupSingleTenantsInDb();
            contexts.RetailDbContext.SetupSingleRetailAndStock();
            contexts.AuthPContext.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(contexts.AuthPContext, new AuthPermissionsOptions
            {
                TenantType = TenantTypes.SingleLevel,
                AppConnectionString = contexts.ConnectionString
            }, new StubRetailTenantChangeServiceFactory(), null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(tenantIds[1]);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = contexts.AuthPContext.Tenants.ToList();
            tenants.Select(x => x.TenantFullName).ShouldEqual(new[] { "Tenant1", "Tenant3" });
            contexts.RetailDbContext.RetailOutlets.IgnoreQueryFilters().Select(x => x.FullName)
                .ToArray().ShouldEqual(new[] { "Tenant1", "Tenant3" });
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsyncBadBecauseUserLinkedToIt()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                builder.UseSqlite(context.Database.GetDbConnection()));
            appOptions.TurnOffDispose();
            var retailContext = new RetailDbContext(appOptions, null);

            var tenantIds = context.SetupSingleTenantsInDb();
            var tenant = context.Find<Tenant>(tenantIds[1]);
            context.Add(new AuthUser("123", "me@gmail.com", "Mr Me", new List<RoleToPermissions>(), tenant));
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel },
                new StubITenantChangeServiceFactory(retailContext), null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(tenant.TenantId);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors().ShouldEqual("This delete is aborted because this tenant is linked to the user 'Mr Me'.");
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsyncErrorFromTenantChangeService()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                builder.UseSqlite(context.Database.GetDbConnection()));
            appOptions.TurnOffDispose();
            var retailContext = new RetailDbContext(appOptions, null);

            var tenantIds = context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel },
                new StubITenantChangeServiceFactory(retailContext, "error from TenantChangeService"), null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(tenantIds[1]);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors().ShouldEqual("error from TenantChangeService");
        }

    }
}