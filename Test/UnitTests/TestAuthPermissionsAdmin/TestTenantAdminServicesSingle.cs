// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using EntityFramework.Exceptions.SqlServer;
using Test.TestHelpers;
using TestSupport.EfHelpers;
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

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions{TenantType = TenantTypes.SingleLevel});

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

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });

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
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });

            //ATTEMPT
            var status = await service.AddSingleTenantAsync("Tenant4");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            tenants.Count.ShouldEqual(4);
            tenants.Last().TenantFullName.ShouldEqual("Tenant4");
        }

        [Fact]
        public async Task TestAddSingleTenantAsyncDuplicate()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });

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
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenantIds = context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });

            //ATTEMPT
            var status = await service.UpdateTenantNameAsync(tenantIds[1], "New Tenant");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            tenants.Count.ShouldEqual(3);
            tenants.Select(x => x.TenantFullName).ShouldEqual(new[] { "Tenant1", "New Tenant", "Tenant3" });
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });
            var deleteLogs = new List<(string fullTenantName, string dataKey)>();

            //ATTEMPT
            var status = await service.DeleteTenantAsync("Tenant2",
                (tuple => deleteLogs.Add(tuple)));

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            deleteLogs.ShouldEqual(new List<(string fullTenantName, string dataKey)>
            {
                ("Tenant2", ".2")
            });

            context.ChangeTracker.Clear();
            var tenants = context.Tenants.ToList();
            tenants.Select(x => x.TenantFullName).ShouldEqual(new[] { "Tenant1", "Tenant3" });
        }


    }
}