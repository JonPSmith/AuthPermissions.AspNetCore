// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using AuthPermissions;
using AuthPermissions.DataLayer.EfCode;
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
        public void TestAddRolesToDatabaseIfEmpty()
        {
            //SETUP
            var services = new ServiceCollection();

            //ATTEMPT
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .SetupForUnitTesting();

            //VERIFY
            using var serviceProvider = services.BuildServiceProvider();
            using var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            context.RoleToPermissions.Count().ShouldEqual(0);
            context.UserToRoles.Count().ShouldEqual(0);
        }

        [Fact]
        public void TestAddRolesToDatabaseIfEmptyOk()
        {
            //SETUP
            var services = new ServiceCollection();
            var lines = @"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One";

            //ATTEMPT
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(lines)
                .SetupForUnitTesting();

            //VERIFY
            using var serviceProvider = services.BuildServiceProvider();
            using var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            context.RoleToPermissions.Count().ShouldEqual(3);
            context.UserToRoles.Count().ShouldEqual(0);
        }

        [Fact]
        public void TestAddRolesToDatabaseIfEmptyAddUsersIfEmptyOk()
        {
            //SETUP
            var services = new ServiceCollection();
            var lines = @"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One";

            //ATTEMPT
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(lines)
                .AddUsersRolesIfEmpty(SetupHelpers.TestUserDefine(), userName => userName)
                .SetupForUnitTesting();

            //VERIFY
            using var serviceProvider = services.BuildServiceProvider();
            using var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            context.RoleToPermissions.Count().ShouldEqual(3);
            context.UserToRoles.Count().ShouldEqual(5);
        }
    }
}
