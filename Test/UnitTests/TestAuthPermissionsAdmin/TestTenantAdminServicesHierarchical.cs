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

            await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });

            //ATTEMPT
            var tenants = service.QueryEndLeafTenants().ToList();

            //VERIFY
            tenants.Count.ShouldEqual(4);
            tenants.Select(x => x.GetTenantName()).OrderBy(x => x).ToArray()
                .ShouldEqual(new[] { "Shop1", "Shop2", "Shop3", "Shop4" });
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
            var newTenant = tenants.SingleOrDefault(x => x.TenantFullName == "Company | West Coast | LA");
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
        public async Task TestUpdateHierarchicalTenantTenantAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenantIds = await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });

            //ATTEMPT
            var status = await service.UpdateTenantNameAsync(tenantIds[1], "West Area");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.Count(x => x.TenantFullName.StartsWith("Company | West Area")).ShouldEqual(4);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncBaseNoActionOk()
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
                "Company | West Coast | SanFran | Shop1", "Company | East Coast | New York");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.Count(x => x.TenantFullName.StartsWith("Company | East Coast | New York | Shop1")).ShouldEqual(1);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncNoActionOk()
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
                "Company | West Coast", "Company | East Coast");

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.Count(x => x.TenantFullName.StartsWith("Company | East Coast | West Coast")).ShouldEqual(4);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncBaseWithActionOkButSaveChangesNotCalled()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });
            var beforeAfterLogs = new List<(string previousDataKey, string newDataKey)>();

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(
                "Company | West Coast | SanFran | Shop1", "Company | East Coast | New York",
                (tuple => beforeAfterLogs.Add(tuple)));

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Message.ShouldEqual("WARNING: It is your job to call the SaveChangesAsync to finish the move to tenant Company | East Coast | New York | Shop1.");
            beforeAfterLogs.ShouldEqual(new List<(string previousDataKey, string newDataKey)>
            {
                (".1.2.5.7", ".1.3.4.7")
            });

            context.ChangeTracker.Clear();
            var tenants = context.Tenants.ToList();
            tenants.Count(x => x.TenantFullName.StartsWith("Company | East Coast | New York | Shop1")).ShouldEqual(0);
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncBaseWithActionOkButSaveChangesCalled()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });
            var beforeAfterLogs = new List<(string previousDataKey, string newDataKey)>();

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(
                "Company | West Coast | SanFran | Shop1", "Company | East Coast | New York",
                (tuple => beforeAfterLogs.Add(tuple)));

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Message.ShouldEqual("WARNING: It is your job to call the SaveChangesAsync to finish the move to tenant Company | East Coast | New York | Shop1.");
            beforeAfterLogs.ShouldEqual(new List<(string previousDataKey, string newDataKey)>
            {
                (".1.2.5.7", ".1.3.4.7")
            });
            status.Result.SaveChanges();

            context.ChangeTracker.Clear();
            var tenants = context.Tenants.ToList();
            tenants.Count(x => x.TenantFullName.StartsWith("Company | East Coast | New York | Shop1")).ShouldEqual(1);
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncWithActionOkButSaveChangesNotCalled()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });
            var beforeAfterLogs = new List<(string previousDataKey, string newDataKey)>();

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(
                "Company | West Coast", "Company | East Coast",
                (tuple => beforeAfterLogs.Add(tuple)));

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Message.ShouldEqual("WARNING: It is your job to call the SaveChangesAsync to finish the move to tenant Company | East Coast | West Coast.");
            beforeAfterLogs.ShouldEqual(new List<(string previousDataKey, string newDataKey)>
            {
                (".1.2", ".1.3.2"),
                (".1.2.5", ".1.3.2.5"),
                (".1.2.5.6", ".1.3.2.5.6"),
                (".1.2.5.7", ".1.3.2.5.7")
            });

            context.ChangeTracker.Clear();
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.Count(x => x.TenantFullName.StartsWith("Company | East Coast | West Coast")).ShouldEqual(0);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncWithActionOkButSaveChangesCalled()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });
            var beforeAfterLogs = new List<(string previousDataKey, string newDataKey)>();

            //ATTEMPT
            var status = await service.MoveHierarchicalTenantToAnotherParentAsync(
                "Company | West Coast", "Company | East Coast",
                (tuple => beforeAfterLogs.Add(tuple)));

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Message.ShouldEqual("WARNING: It is your job to call the SaveChangesAsync to finish the move to tenant Company | East Coast | West Coast.");
            beforeAfterLogs.ShouldEqual(new List<(string previousDataKey, string newDataKey)>
            {
                (".1.2", ".1.3.2"),
                (".1.2.5", ".1.3.2.5"),
                (".1.2.5.6", ".1.3.2.5.6"),
                (".1.2.5.7", ".1.3.2.5.7")
            });
            status.Result.SaveChanges();

            context.ChangeTracker.Clear();
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.Count(x => x.TenantFullName.StartsWith("Company | East Coast | West Coast")).ShouldEqual(4);
        }

        [Fact]
        public async Task TestMoveHierarchicalTenantToAnotherParentAsyncMoveToChild()
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
                "Company | West Coast", "Company | West Coast | SanFran | Shop2");

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("You cannot move a tenant one of its children.");
        }

        [Fact]
        public async Task TestDeleteTenantAsyncBaseOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenantNames = await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });
            var deleteLogs = new List<(string fullTenantName, string dataKey)>();

            //ATTEMPT
            var status = await service.DeleteTenantAsync("Company | West Coast | SanFran | Shop1",
                (tuple => deleteLogs.Add(tuple)));

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            deleteLogs.ShouldEqual(new List<(string fullTenantName, string dataKey)>
            {
                ("Company | West Coast | SanFran | Shop1", ".1.2.5.7")
            });

            context.ChangeTracker.Clear();
            var tenants = context.Tenants.ToList();
            foreach (var tenant in tenants.OrderBy(x => x.GetTenantDataKey()))
            {
                _output.WriteLine(tenant.ToString());
            }
            tenants.SingleOrDefault(x => x.TenantFullName == "Company | West Coast | SanFran | Shop1").ShouldBeNull();
            tenants.Count.ShouldEqual(tenantNames.Count - 1);
        }

        [Fact]
        public async Task TestDeleteTenantAsyncAnotherParentOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenantNames = await context.SetupHierarchicalTenantInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, new AuthPermissionsOptions { TenantType = TenantTypes.HierarchicalTenant });
            var deleteLogs = new List<(string fullTenantName, string dataKey)>();

            //ATTEMPT
            var status = await service.DeleteTenantAsync("Company | West Coast",
                (tuple => deleteLogs.Add(tuple)));

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            deleteLogs.ShouldEqual(new List<(string fullTenantName, string dataKey)>
            {
                ("Company | West Coast", ".1.2"),
                ("Company | West Coast | SanFran", ".1.2.5"),
                ("Company | West Coast | SanFran | Shop2", ".1.2.5.6"),
                ("Company | West Coast | SanFran | Shop1", ".1.2.5.7")
            });
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
}