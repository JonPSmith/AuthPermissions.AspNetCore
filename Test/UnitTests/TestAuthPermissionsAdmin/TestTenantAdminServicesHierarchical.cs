// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Example4.ShopCode.EfCoreCode;
using Microsoft.EntityFrameworkCore;
using Test.StubClasses;
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

        private readonly AuthPermissionsOptions _authOptionsHierarchical =
            new() { TenantType = TenantTypes.HierarchicalTenant };

        public TestTenantAdminServicesHierarchical(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestSetupHierarchicalTenantInDbAsync()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            //ATTEMPT
            await context.BulkLoadHierarchicalTenantInDbAsync();

            //VERIFY
            context.ChangeTracker.Clear();
            var tenants = context.Tenants.OrderBy(x => x.TenantId).ToList();
            foreach (var tenant in tenants)
            {
                _output.WriteLine(tenant.ToString());
            }
        }

        [Fact]
        public async Task TestQueryEndLeafTenantsHierarchical()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            await context.BulkLoadHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, _authOptionsHierarchical, 
                "en".SetupAuthPLoggingLocalizer(), null, null);

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

            var tenantIds = await context.BulkLoadHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                "en".SetupAuthPLoggingLocalizer(), null, null);

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
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var appOptions = SqliteInMemory.CreateOptions<RetailDbContext>(builder =>
                    builder.UseSqlite(context.Database.GetDbConnection()));
                appOptions.TurnOffDispose();
                var retailContext = new RetailDbContext(appOptions, null);

                var tenantIds = await context.BulkLoadHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

                //ATTEMPT
                var status = await service.AddHierarchicalTenantAsync("LA", tenantIds[1]);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                subTenantChangeService.NewTenantName.ShouldEqual("Company | West Coast | LA");
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                var tenants = context.Tenants.ToList();
                var newTenant = tenants.SingleOrDefault(x => x.TenantFullName == "Company | West Coast | LA");
                tenants.Count.ShouldEqual(10);
                newTenant.ShouldNotBeNull();
                newTenant.GetTenantDataKey().ShouldEqual("1.2.10.");
            }
        }

        [Fact]
        public async Task TestAddHierarchicalTenantAsyncWithRolesOk()
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

                var role1 = new RoleToPermissions("TenantRole1", null, $"{(char)1}{(char)3}", RoleTypes.TenantAutoAdd);
                var role2 = new RoleToPermissions("TenantRole2", null, $"{(char)2}{(char)3}", RoleTypes.TenantAdminAdd);
                context.AddRange(role1, role2);
                context.SaveChanges();

                var tenantIds = await context.BulkLoadHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

                //ATTEMPT
                var status = await service.AddHierarchicalTenantAsync("LA", tenantIds[1], new List<string> { "TenantRole1", "TenantRole2" });

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                subTenantChangeService.NewTenantName.ShouldEqual("Company | West Coast | LA");
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                var tenants = context.Tenants.Include(x => x.TenantRoles).ToList();
                var newTenant = tenants.SingleOrDefault(x => x.TenantFullName == "Company | West Coast | LA");
                newTenant.TenantRoles.Select(x => x.RoleName).ShouldEqual(new string[] { "TenantRole1", "TenantRole2" }); ;
            }
        }

        [Theory]
        [InlineData("BadName", "The Role 'BadName' was not found in the lists of Roles.")]
        [InlineData("NormalRole", "The Role 'NormalRole' is not a tenant role")]
        public async Task TestAddHierarchicalTenantAsyncWithRolesBad(string roleName, string errorStart)
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

                context.Add(new RoleToPermissions("NormalRole", null, $"{(char)1}{(char)3}"));
                context.SaveChanges();

                var tenantIds = await context.BulkLoadHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

                //ATTEMPT
                var status = await service.AddHierarchicalTenantAsync("LA", tenantIds[1], new List<string> { roleName });

                //VERIFY
                status.IsValid.ShouldBeFalse();
                _output.WriteLine(status.GetAllErrors());
                status.GetAllErrors().ShouldStartWith(errorStart);
            }
        }

        [Fact]
        public async Task TestAddHierarchicalTenantAsyncTopLevelOk()
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

                var tenantIds = await context.BulkLoadHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

                //ATTEMPT
                var status = await service.AddHierarchicalTenantAsync("New Company", 0);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                subTenantChangeService.NewTenantName.ShouldEqual("New Company");
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                var tenants = context.Tenants.ToList();
                var newTenant = tenants.SingleOrDefault(x => x.TenantFullName == "New Company");
                tenants.Count.ShouldEqual(10);
                newTenant.ShouldNotBeNull();
                newTenant.GetTenantDataKey().ShouldEqual("10.");
            }
        }

        [Fact]
        public async Task TestAddHierarchicalTenantAsyncDuplicate()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);
            var tenantIds = await contexts.AuthPContext.BulkLoadHierarchicalTenantInDbAsync();
            contexts.RetailDbContext.SetupHierarchicalRetailAndStock(contexts.AuthPContext);

            var subTenantChangeService = new StubTenantChangeServiceFactory();
            var service = new AuthTenantAdminService(contexts.AuthPContext, _authOptionsHierarchical,
                "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

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

                var tenantIds = await context.BulkLoadHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

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

                await context.BulkLoadHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

                //ATTEMPT
                var status = await service.MoveHierarchicalTenantToAnotherParentAsync(6, 5);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Message.ShouldEqual("Successfully moved the tenant originally named 'Company | West Coast | SanFran | Shop1' to the new named 'Company | East Coast | New York | Shop1'.");
                subTenantChangeService.MoveReturnedTuples
                    .ShouldEqual(new List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)>
                {
                    ("1.2.4.6.", "1.3.5.6.", 6, "Company | East Coast | New York | Shop1")
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

                await context.BulkLoadHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

                //ATTEMPT
                var status = await service.MoveHierarchicalTenantToAnotherParentAsync(2, 3);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Message.ShouldEqual("Successfully moved the tenant originally named 'Company | West Coast' to the new named 'Company | East Coast | West Coast'.");
                subTenantChangeService.MoveReturnedTuples
                    .ShouldEqual(new List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)>
                {
                    ("1.2.", "1.3.2.", 2, "Company | East Coast | West Coast"),
                    ("1.2.4.", "1.3.2.4.", 4, "Company | East Coast | West Coast | SanFran"),
                    ("1.2.4.6.", "1.3.2.4.6.", 6, "Company | East Coast | West Coast | SanFran | Shop1"),
                    ("1.2.4.7.", "1.3.2.4.7.", 7, "Company | East Coast | West Coast | SanFran | Shop2")
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

                await context.BulkLoadHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var subTenantChangeService = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

                //ATTEMPT
                var status = await service.MoveHierarchicalTenantToAnotherParentAsync(3, 0);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                status.Message.ShouldEqual("Successfully moved the tenant originally named 'Company | East Coast' to top level.");
                subTenantChangeService.MoveReturnedTuples
                    .ShouldEqual(new List<(string oldDataKey, string newDataKey, int tenantId, string newFullTenantName)>
                {
                    ("1.3.", "3.", 3, "East Coast"), 
                    ("1.3.5.", "3.5.", 5, "East Coast | New York"), 
                    ("1.3.5.8.", "3.5.8.", 8, "East Coast | New York | Shop3"), 
                    ("1.3.5.9.", "3.5.9.", 9, "East Coast | New York | Shop4")
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
        [InlineData(4)]
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

            await context.BulkLoadHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var subTenantChangeService = new StubTenantChangeServiceFactory();
            var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

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

            await context.BulkLoadHierarchicalTenantInDbAsync();
            context.ChangeTracker.Clear();

            var subTenantChangeService = new StubTenantChangeServiceFactory();
            var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                "en".SetupAuthPLoggingLocalizer(), subTenantChangeService, null);

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

                numTenants = (await context.BulkLoadHierarchicalTenantInDbAsync()).Count;
                context.ChangeTracker.Clear();

                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), new StubTenantChangeServiceFactory(), null);

                //ATTEMPT
                var status = await service.DeleteTenantAsync(6);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                var deleteLogs = ((StubTenantChangeServiceFactory.StubITenantChangeService)status.Result).DeleteReturnedTuples;
                deleteLogs.ShouldEqual(new List<(string fullTenantName, string dataKey)>
                {
                    ("Company | West Coast | SanFran | Shop1", "1.2.4.6.")
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

                var tenantIds = await context.BulkLoadHierarchicalTenantInDbAsync();
                context.ChangeTracker.Clear();

                var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                    "en".SetupAuthPLoggingLocalizer(), new StubTenantChangeServiceFactory(), null);
                options.StopNextDispose();

                //ATTEMPT
                var status = await service.DeleteTenantAsync(2);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                var deleteLogs = ((StubTenantChangeServiceFactory.StubITenantChangeService)status.Result).DeleteReturnedTuples;
                deleteLogs.ShouldEqual(new List<(string fullTenantName, string dataKey)>
                {
                    ("Company | West Coast | SanFran | Shop1", "1.2.4.6."), 
                    ("Company | West Coast | SanFran | Shop2", "1.2.4.7."), 
                    ("Company | West Coast | SanFran", "1.2.4."), 
                    ("Company | West Coast", "1.2.")
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

            await context.BulkLoadHierarchicalTenantInDbAsync();
            var tenantToDelete = context.Find<Tenant>(7);
            context.Add(AuthPSetupHelpers.CreateTestAuthUserOk("123", "me@gmail.com", "Mr Me", 
                new List<RoleToPermissions>(), tenantToDelete));
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                "en".SetupAuthPLoggingLocalizer(), new StubTenantChangeServiceFactory(), null);
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

            await context.BulkLoadHierarchicalTenantInDbAsync();
            var childTenant = context.Find<Tenant>(7);
            context.Add(AuthPSetupHelpers.CreateTestAuthUserOk("123", "me@gmail.com", "Mr Me",
                new List<RoleToPermissions>(), childTenant));
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, _authOptionsHierarchical,
                "en".SetupAuthPLoggingLocalizer(), new StubTenantChangeServiceFactory(), null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(2);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors().ShouldEqual("This delete is aborted because this tenant or its children tenants are linked to the user 'Mr Me'.");
        }
    }
}