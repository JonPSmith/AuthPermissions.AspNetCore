// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.Extensions.DependencyInjection;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestSetupExtensionsSetupForUnitTesting
    {
        private readonly ITestOutputHelper _output;

        public TestSetupExtensionsSetupForUnitTesting(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestAddRolesToDatabaseIfEmpty()
        {
            //SETUP
            var services = new ServiceCollection();

            //ATTEMPT
            var serviceProvider = await services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .SetupForUnitTestingAsync();
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            //VERIFY
            context.ChangeTracker.Clear();
            context.AuthUsers.Count().ShouldEqual(0);
            context.RoleToPermissions.Count().ShouldEqual(0);
            context.UserToRoles.Count().ShouldEqual(0);
        }

        [Fact]
        public async Task AddRolesToDatabaseIfEmptyOk()
        {
            //SETUP
            var services = new ServiceCollection();
            var lines = @"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One";

            //ATTEMPT
            var serviceProvider = await services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(lines)
                .SetupForUnitTestingAsync();
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            //VERIFY
            context.ChangeTracker.Clear();
            context.AuthUsers.Count().ShouldEqual(0);
            context.RoleToPermissions.Count().ShouldEqual(3);
            context.UserToRoles.Count().ShouldEqual(0);
            context.AuthUsers.Count(x => x.TenantId != null).ShouldEqual(0);
        }

        [Fact]
        public async Task AddRolesToDatabaseIfEmptyAddUsersIfEmptyOk()
        {
            //SETUP
            var services = new ServiceCollection();
            var lines = @"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One";

            //ATTEMPT
            var serviceProvider = await services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(lines)
                .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
                .AddAuthUsersIfEmpty(SetupHelpers.TestUserDefineWithUserId())
                .SetupForUnitTestingAsync();
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            //VERIFY
            context.ChangeTracker.Clear();
            context.AuthUsers.Count().ShouldEqual(3);
            context.RoleToPermissions.Count().ShouldEqual(3);
            context.UserToRoles.Count().ShouldEqual(5);
            context.AuthUsers.Count(x => x.TenantId != null).ShouldEqual(0);
        }

        [Fact]
        public async Task AddRolesToDatabaseIfEmptyAddTenantsAddUsersIfEmptyOk()
        {
            //SETUP
            var services = new ServiceCollection();
            var rolesLines = @"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One";
            var tenantLines = @"Tenant1
Tenant2
Tenant3";

            //ATTEMPT
            var serviceProvider = await services.RegisterAuthPermissions<TestEnum>(options =>
                {
                    options.TenantType = TenantTypes.SingleLevel;
                })
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(rolesLines)
                .AddTenantsIfEmpty(tenantLines)
                .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
                .AddAuthUsersIfEmpty(SetupHelpers.TestUserDefineWithTenants())
                .SetupForUnitTestingAsync();
            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            //VERIFY
            context.ChangeTracker.Clear();
            context.AuthUsers.Count().ShouldEqual(3);
            context.RoleToPermissions.Count().ShouldEqual(3);
            context.UserToRoles.Count().ShouldEqual(5);
            context.Tenants.Count().ShouldEqual(3);
            context.AuthUsers.Count(x => x.TenantId != null).ShouldEqual(3);
        }
    }
}
