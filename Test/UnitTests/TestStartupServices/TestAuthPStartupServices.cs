// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;
using Test.DiTestHelpers;
using Test.TestHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestStartupServices
{
    public class TestAuthPStartupServices
    {
        private readonly ITestOutputHelper _output;

        public TestAuthPStartupServices(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestStartupServiceBulkLoadAuthPInfo()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>(options => options.TenantType = TenantTypes.SingleLevel)
            .UsingInMemoryDatabase()
            .AddRolesPermissionsIfEmpty(@"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One")
            .AddTenantsIfEmpty(@"Tenant1
Tenant2
Tenant3")
            .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
            .AddAuthUsersIfEmpty(AuthPSetupHelpers.TestUserDefineWithUserId())
            .SetupAspNetCoreAndDatabase();
            var serviceProvider = services.BuildServiceProvider();

            //ATTEMPT
            var startupService = serviceProvider.GetServices<IStartupServiceToRunSequentially>().Single();
            await startupService.ApplyYourChangeAsync(serviceProvider);

            //VERIFY
            using var authContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            foreach (var authUser in authContext.AuthUsers.ToList())
            {
                _output.WriteLine(authUser.ToString());
            }
            authContext.AuthUsers.Count().ShouldEqual(3);
            authContext.RoleToPermissions.Count().ShouldEqual(3);
            authContext.UserToRoles.Count().ShouldEqual(5);
            authContext.Tenants.Count().ShouldEqual(3);
        }

        [Fact]
        public async Task TestStartupServiceIndividualAccountsAddSuperUser()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>(options => options.TenantType = TenantTypes.SingleLevel)
                .UsingInMemoryDatabase()
                .IndividualAccountsAuthentication()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase();
            var serviceProvider = services.BuildServiceProvider();

            //ATTEMPT
            var startupService = serviceProvider.GetServices<IStartupServiceToRunSequentially>().Single();
            await startupService.ApplyYourChangeAsync(serviceProvider);

            //VERIFY
            using var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            userManager.Users.Count().ShouldEqual(1);
        }

        [Fact]
        public async Task TestStartupServiceMigrateAnyDbContext()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            var serviceProvider = services.BuildServiceProvider();

            var startupService = new StartupServiceMigrateAnyDbContext<ApplicationDbContext>();

            //ATTEMPT
            await startupService.ApplyYourChangeAsync(serviceProvider);

            //VERIFY
        }

        [Fact]
        public async Task TestStartupServiceMigrateAuthPDatabase()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>(options => options.PathToFolderToLock = TestData.GetTestDataDir())
                .IndividualAccountsAuthentication()
                .UsingEfCoreSqlServer(this.GetUniqueDatabaseConnectionString())
                .SetupAspNetCoreAndDatabase();
            var serviceProvider = services.BuildServiceProvider();

            var startupService = new StartupServiceMigrateAuthPDatabase();

            //ATTEMPT
            await startupService.ApplyYourChangeAsync(serviceProvider);

            //VERIFY
        }        
        
        [Fact]
        public async Task TestStartupServicesIndividualAccountsAddDemoUsers()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            var serviceProvider = services.BuildServiceProvider();

            var startupService = new StartupServicesIndividualAccountsAddDemoUsers();

            //ATTEMPT
            await startupService.ApplyYourChangeAsync(serviceProvider);

            //VERIFY
            using var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            userManager.Users.Count().ShouldEqual(3);
        }
    }
}