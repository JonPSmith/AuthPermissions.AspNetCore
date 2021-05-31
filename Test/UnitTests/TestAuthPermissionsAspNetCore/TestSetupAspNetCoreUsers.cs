// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.HostedServices;
using AuthPermissions.DataLayer.EfCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Test.DiTestHelpers;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAspNetCore
{
    public class TestAspNetSetupExtension
    {
        private readonly ITestOutputHelper _output;

        public TestAspNetSetupExtension(ITestOutputHelper output)
        {
            _output = output;
        }


        [Fact]
        public async Task TestSetupAspNetCoreSetupAspNetCorePartNoHostedService()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .SetupAspNetCorePart();

            var serviceProvider = services.BuildServiceProvider();

            //ATTEMPT
            var startupServices = serviceProvider.GetServices<IHostedService>().ToList();
            startupServices.Count().ShouldEqual(1);
            startupServices.Single().GetType().Name.ShouldEqual("DataProtectionHostedService");
            await startupServices.Last().StartAsync(default);

            //VERIFY
            using var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            userManager.Users.Count().ShouldEqual(0);
        }

        [Fact]
        public async Task TestSetupAspNetCoreSetupAspNetCorePartIncludeAddRolesUsers()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .SetupAspNetCorePart(true);

            var serviceProvider = services.BuildServiceProvider();

            //ATTEMPT
            var startupServices = serviceProvider.GetServices<IHostedService>().ToList();
            startupServices.Count().ShouldEqual(2);
            startupServices.Last().ShouldBeType<AddAuthRolesUserOnStartup>();
            await startupServices.Last().StartAsync(default);

            //VERIFY
            var authContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            authContext.UserToRoles.Count().ShouldEqual(0);
            using var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            userManager.Users.Count().ShouldEqual(0);
        }

        [Fact]
        public async Task TestSetupAspNetCoreIndividualAccountsAddSuperUser()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .IndividualAccountsAddSuperUser()
                .SetupAspNetCorePart();

            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IHostedService>().ToList();

            //ATTEMPT
            startupServices.Count().ShouldEqual(2);
            startupServices.Last().ShouldBeType<IndividualAccountsAddSuperUser>();
            await startupServices.Last().StartAsync(default);

            //VERIFY
            using var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            userManager.Users.Count().ShouldEqual(1);
        }

        [Fact]
        public async Task TestSetupAspNetCoreSetupAuthDatabaseOnStartup()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .SetupAuthDatabaseOnStartup();

            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IHostedService>().ToList();

            //ATTEMPT
            startupServices.Count().ShouldEqual(3);
            startupServices[1].ShouldBeType<SetupDatabaseOnStartup>();
            await startupServices[1].StartAsync(default);

            //VERIFY
            var authContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            authContext.UserToRoles.Count().ShouldEqual(0);
        }

        [Fact]
        public async Task TestSetupAspNetCoreAddRolesPermissionsUsersIfEmpty()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(@"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One")
                .AddUsersRolesIfEmpty(SetupHelpers.TestUserDefineWithUserId())
                .SetupAuthDatabaseOnStartup();

            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IHostedService>().ToList();

            //ATTEMPT
            startupServices.Count().ShouldEqual(3);
            startupServices[1].ShouldBeType<SetupDatabaseOnStartup>();
            await startupServices[1].StartAsync(default);
            startupServices[2].ShouldBeType<AddAuthRolesUserOnStartup>();
            await startupServices[2].StartAsync(default);

            //VERIFY
            var authContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            authContext.RoleToPermissions.Count().ShouldEqual(3);
            authContext.UserToRoles.Count().ShouldEqual(5);
        }

        [Fact]
        public async Task TestSetupAspNetCoreSuperUserAddRolesPermissionsUsersIfEmpty()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(@"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One")
                .AddUsersRolesIfEmpty(SetupHelpers.TestUserDefineWithUserId())
                .IndividualAccountsAddSuperUser()
                .SetupAuthDatabaseOnStartup();

            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IHostedService>().ToList();

            //ATTEMPT
            startupServices.Count().ShouldEqual(4);
            startupServices[1].ShouldBeType<IndividualAccountsAddSuperUser>();
            await startupServices[1].StartAsync(default);
            startupServices[2].ShouldBeType<SetupDatabaseOnStartup>();
            await startupServices[2].StartAsync(default);
            startupServices[3].ShouldBeType<AddAuthRolesUserOnStartup>();
            await startupServices[3].StartAsync(default);

            //VERIFY
            var authContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            authContext.RoleToPermissions.Count().ShouldEqual(3);
            authContext.UserToRoles.Count().ShouldEqual(5);
        }
    }
}