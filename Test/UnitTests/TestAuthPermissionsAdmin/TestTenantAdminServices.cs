// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using EntityFramework.Exceptions.SqlServer;
using Microsoft.EntityFrameworkCore;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestTenantAdminServices
    {
        private ITestOutputHelper _output;

        public TestTenantAdminServices(ITestOutputHelper output)
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

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions{TenantType = TenantTypes.SingleTenant});

            //ATTEMPT
            var tenants = service.QueryTenants().ToList();

            //VERIFY
            tenants.Count.ShouldEqual(3);
            tenants.Select(x => x.TenantName).ShouldEqual(new[]{ "Tenant1", "Tenant2", "Tenant3" });
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

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

            //ATTEMPT
            var status = await service.AddSingleTenantAsync("Tenant4");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            tenants.Count.ShouldEqual(4);
            tenants.Last().TenantName.ShouldEqual("Tenant4");
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

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

            //ATTEMPT
            var status = await service.AddSingleTenantAsync("Tenant2");

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("There is already a Tenant with a value: name = Tenant2");
        }

        [Fact]
        public async Task TestAddHierarchicalTenantAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenantNames = await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });

            //ATTEMPT
            var status = await service.AddHierarchicalTenantAsync("LA", "Company | West Coast");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            var newTenant = tenants.SingleOrDefault(x => x.TenantName == "Company | West Coast | LA");
            tenants.Count.ShouldEqual(10);
            newTenant.ShouldNotBeNull();
            newTenant.GetTenantDataKey().ShouldEqual(".1.2.10");
        }

        [Fact]
        public async Task TestAddHierarchicalTenantAsyncDuplicate()
        {
            //SETUP
            var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
                builder.UseExceptionProcessor());
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureClean();

            var tenantNames = await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });

            //ATTEMPT
            var status = await service.AddHierarchicalTenantAsync("West Coast", "Company");

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("There is already a Tenant with a value: name = Company | West Coast");
        }

        [Fact]
        public async Task TestUpdateSingleTenantAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleTenant });

            //ATTEMPT
            var status = await service.UpdateTenantNameAsync("Tenant2", "New Tenant");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            tenants.Count.ShouldEqual(3);
            tenants.Select(x => x.TenantName).ShouldEqual(new[] { "Tenant1", "New Tenant", "Tenant3" });
        }

        [Fact]
        public async Task TestUpdateHierarchicalTenantTenantAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();
            //var options = this.CreateUniqueClassOptions<AuthPermissionsDbContext>(builder =>
            //    builder.UseExceptionProcessor());
            //using var context = new AuthPermissionsDbContext(options);
            //context.Database.EnsureClean();

            await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });

            //ATTEMPT
            var status = await service.UpdateTenantNameAsync("Company | West Coast", "West Area");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.Count(x => x.TenantName.StartsWith("Company | West Area")).ShouldEqual(4);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(
                "Company | West Coast | SanFran", "Company | East Coast");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.Count(x => x.TenantName.StartsWith("Company | East Coast | SanFran")).ShouldEqual(3);
        }
    }
}