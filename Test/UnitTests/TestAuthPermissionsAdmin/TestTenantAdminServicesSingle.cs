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
using Microsoft.EntityFrameworkCore;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin
{
    public class TestTenantAdminServicesSingle
    {
        private readonly AuthPermissionsOptions _authOptionsSingle =
            new() { TenantType = TenantTypes.SingleLevel };

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

            var service = new AuthTenantAdminService(context, _authOptionsSingle, 
                "en".SetupAuthPLoggingLocalizer(),null, null);

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

            var service = new AuthTenantAdminService(context, _authOptionsSingle,
                "en".SetupAuthPLoggingLocalizer(), null, null);

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

                context.SetupSingleTenantsInDb();
                context.ChangeTracker.Clear();

                var tenantChange = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsSingle,
                    "en".SetupAuthPLoggingLocalizer(), tenantChange, null);

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
        public async Task TestGetRoleNamesForTenantsAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var role1 = new RoleToPermissions("AutoAddRole", null, $"{(char)1}{(char)3}", RoleTypes.TenantAutoAdd);
                var role2 = new RoleToPermissions("AdminAddRole", null, $"{(char)2}{(char)3}", RoleTypes.TenantAdminAdd);
                var role3 = new RoleToPermissions("NormalRole", null, $"{(char)2}{(char)3}");
                context.AddRange(role1, role2, role3);
                context.SaveChanges();
                context.ChangeTracker.Clear();

                var service = new AuthTenantAdminService(context, _authOptionsSingle,
                    "en".SetupAuthPLoggingLocalizer(), null, null);

                //ATTEMPT
                var roleNames = await service.GetRoleNamesForTenantsAsync();

                //VERIFY
                roleNames.ShouldEqual(new List<string> { "AutoAddRole", "AdminAddRole" });
            }
        }

        [Fact]
        public async Task TestAddSingleTenantAsyncWithRolesOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var role1 = new RoleToPermissions("TenantRole1", null, $"{(char)1}{(char)3}", RoleTypes.TenantAutoAdd);
                var role2 = new RoleToPermissions("TenantRole2", null, $"{(char)2}{(char)3}", RoleTypes.TenantAdminAdd);
                context.AddRange(role1, role2);
                context.SaveChanges();
                var tenantIds = context.SetupSingleTenantsInDb();
                context.ChangeTracker.Clear();

                var tenantChange = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsSingle,
                    "en".SetupAuthPLoggingLocalizer(), tenantChange, null);

                //ATTEMPT
                var status = await service.AddSingleTenantAsync("Tenant4", new List<string>{"TenantRole1", "TenantRole2"});

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                tenantChange.NewTenantName.ShouldEqual("Tenant4");
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                var tenants = context.Tenants.Include(x => x.TenantRoles).ToList();
                tenants.Count.ShouldEqual(4);
                tenants.Last().TenantRoles.Select(x => x.RoleName).ShouldEqual(new string[] { "TenantRole1", "TenantRole2" });
            }
        }

        [Theory]
        [InlineData("BadName", "The Role 'BadName' was not found in the lists of Roles.")]
        [InlineData("NormalRole", "The Role 'NormalRole' is not a tenant role")]
        public async Task TestAddSingleTenantAsyncWithRolesBad(string roleName, string errorStart)
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                context.Add(new RoleToPermissions("NormalRole", null, $"{(char)1}{(char)3}"));
                context.SaveChanges();
                var tenantIds = context.SetupSingleTenantsInDb();
                context.ChangeTracker.Clear();

                var tenantChange = new StubTenantChangeServiceFactory();
                var service = new AuthTenantAdminService(context, _authOptionsSingle,
                    "en".SetupAuthPLoggingLocalizer(), tenantChange, null);

                //ATTEMPT
                var status = await service.AddSingleTenantAsync("Tenant4", new List<string> { roleName });

                //VERIFY
                status.IsValid.ShouldBeFalse();
                _output.WriteLine(status.GetAllErrors());
                status.GetAllErrors().ShouldStartWith(errorStart);
            }
        }

        // The UpdateTenantRolesAsync is used on both single and Hierarchical tenants, so only needs one test
        [Fact]
        public async Task TestUpdateTenantRolesAsync()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();
            var role1 = new RoleToPermissions("TenantRole1", null, $"{(char)1}{(char)3}", RoleTypes.TenantAutoAdd);
            var role2 = new RoleToPermissions("TenantRole2", null, $"{(char)2}{(char)3}", RoleTypes.TenantAdminAdd);
            context.AddRange(role1, role2);
            var newTenant =
                AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1", new List<RoleToPermissions> { role1 });
            context.Add(newTenant);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, _authOptionsSingle,
                "en".SetupAuthPLoggingLocalizer(), null, null);
            //ATTEMPT
            var status = await service.UpdateTenantRolesAsync(newTenant.TenantId, new List<string> { "TenantRole2" });

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.ChangeTracker.Clear();
            var updatedTenant = context.Tenants.Include(x => x.TenantRoles).Single();
            updatedTenant.TenantRoles.Select(x => x.RoleName).ShouldEqual(new string[]{ "TenantRole2" });
        }

        [Fact]
        public async Task TestAddSingleTenantAsyncDuplicate()
        {
            //SETUP
            using var contexts = new HierarchicalTenantChangeSqlServerSetup(this);
            var tenantIds = contexts.AuthPContext.SetupSingleTenantsInDb();
            contexts.RetailDbContext.SetupSingleRetailAndStock();
            contexts.AuthPContext.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(contexts.AuthPContext, _authOptionsSingle,
                "en".SetupAuthPLoggingLocalizer(), 
                new StubRetailTenantChangeServiceFactory(contexts.RetailDbContext), null);

            //ATTEMPT
            var status = await service.AddSingleTenantAsync("Tenant2");

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("There is already a Tenant with a value: name = Tenant2");
        }

        [Fact]
        public async Task TestUpdateNameSingleTenantAsyncOk()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var tenantIds = context.SetupSingleTenantsInDb();
                context.ChangeTracker.Clear();

                var service = new AuthTenantAdminService(context, _authOptionsSingle,
                    "en".SetupAuthPLoggingLocalizer(),
                    new StubTenantChangeServiceFactory(), null);

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
        public async Task TestDeleteSingleTenantAsync()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            options.TurnOffDispose();
            using (var context = new AuthPermissionsDbContext(options))
            {
                context.Database.EnsureCreated();

                var tenantIds = context.SetupSingleTenantsInDb();
                context.ChangeTracker.Clear();

                var service = new AuthTenantAdminService(context, _authOptionsSingle,
                    "en".SetupAuthPLoggingLocalizer(),
                    new StubTenantChangeServiceFactory(), null);

                //ATTEMPT
                var status = await service.DeleteTenantAsync(tenantIds[1]);

                //VERIFY
                status.IsValid.ShouldBeTrue(status.GetAllErrors());
                var deleteLogs = ((StubTenantChangeServiceFactory.StubITenantChangeService)status.Result).DeleteReturnedTuples; 
                deleteLogs.ShouldEqual(new List<(string dataKey, string fullTenantName)>
                {
                    ("2.", "Tenant2")
                });
            }
            using (var context = new AuthPermissionsDbContext(options))
            {
                var tenants = context.Tenants.ToList();
                tenants.Select(x => x.TenantFullName).ShouldEqual(new[] { "Tenant1", "Tenant3" });
            }
        }

        [Fact]
        public async Task TestDeleteSingleTenantAsyncBadBecauseUserLinkedToIt()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenantIds = context.SetupSingleTenantsInDb();
            var tenant = context.Find<Tenant>(tenantIds[1]);
            context.Add(AuthPSetupHelpers.CreateTestAuthUserOk("123", "me@gmail.com", "Mr Me",
                new List<RoleToPermissions>(), tenant));
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, _authOptionsSingle,
                "en".SetupAuthPLoggingLocalizer(),
                new StubTenantChangeServiceFactory(), null);

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

            var tenantIds = context.SetupSingleTenantsInDb();
            context.ChangeTracker.Clear();

            var service = new AuthTenantAdminService(context, _authOptionsSingle,
                "en".SetupAuthPLoggingLocalizer(),
                new StubTenantChangeServiceFactory("error from TenantChangeService"), null);

            //ATTEMPT
            var status = await service.DeleteTenantAsync(tenantIds[1]);

            //VERIFY
            status.IsValid.ShouldBeFalse(status.GetAllErrors());
            status.GetAllErrors().ShouldEqual("error from TenantChangeService");
        }

    }
}