// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RunMethodsSequentially;
using Test.DiTestHelpers;
using Test.TestHelpers;
using TestSupport.Helpers;
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
        public void TestSetupAspNetCoreSetupAspNetCorePartHostedService()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .SetupAspNetCorePart();

            //ATTEMPT
            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IHostedService>().ToList();


            //VERIFY
            startupServices.Count.ShouldEqual(2);
            startupServices.First().GetType().Name.ShouldEqual("DataProtectionHostedService");
            startupServices.Last().GetType().Name.ShouldEqual("GetLockAndThenRunHostedService");
        }

        [Fact]
        public void TestSetupAspNetCoreSetupAspNetCoreAndDatabase_Migrate()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .IndividualAccountsAuthentication()
                .SetupAspNetCoreAndDatabase();

            //ATTEMPT
            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //VERIFY
            startupServices.Count.ShouldEqual(1);
            startupServices.Single().ShouldBeType<StartupServiceMigrateAuthPDatabase>();
        }

        [Fact]
        public async Task TestSetupAspNetCoreIndividualAccountsAddSuperUser()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .IndividualAccountsAuthentication()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase();

            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //ATTEMPT
            startupServices.Count.ShouldEqual(2);
            startupServices.First().ShouldBeType<StartupServiceIndividualAccountsAddSuperUser<IdentityUser>>();
            startupServices.Last().ShouldBeType<StartupServiceMigrateAuthPDatabase>();
            await startupServices.Last().ApplyYourChangeAsync(serviceProvider);

            //VERIFY
            using var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            userManager.Users.Count().ShouldEqual(1);
        }

        [Fact]
        public async Task TestSetupAspNetCoreIndividualAccountsAddSuperUser_CustomIdentityUser()
        {
            //SETUP
            var services = this.SetupServicesForTestCustomIdentityUser();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .IndividualAccountsAuthentication<CustomIdentityUser>()
                .AddSuperUserToIndividualAccounts<CustomIdentityUser>()
                .SetupAspNetCoreAndDatabase();

            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //ATTEMPT
            startupServices.Count.ShouldEqual(2);
            startupServices.First().ShouldBeType<StartupServiceIndividualAccountsAddSuperUser<CustomIdentityUser>>();
            startupServices.Last().ShouldBeType<StartupServiceMigrateAuthPDatabase>();
            await startupServices.Last().ApplyYourChangeAsync(serviceProvider);

            //VERIFY
            using var userManager = serviceProvider.GetRequiredService<UserManager<CustomIdentityUser>>();
            userManager.Users.Count().ShouldEqual(1);
        }

        [Fact]
        public void TestSetupAspNetCoreSetupAuthDatabaseOnStartup()
        {
            //SETUP
            var aspNetConnectionString = this.GetUniqueDatabaseConnectionString();
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .IndividualAccountsAuthentication()
                .UsingEfCoreSqlServer(aspNetConnectionString)
                .SetupAspNetCoreAndDatabase(options =>
                {
                    //Migrate individual account database
                    options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
                });

            //ATTEMPT
            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //VERIFY
            startupServices.Count.ShouldEqual(1);
            startupServices[1].ShouldBeType<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
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
                .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
                .AddAuthUsersIfEmpty(AuthPSetupHelpers.TestUserDefineWithUserId())
                .SetupAspNetCoreAndDatabase();

            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //ATTEMPT
            startupServices.Count.ShouldEqual(2);
            startupServices[1].ShouldBeType<StartupServiceBulkLoadAuthPInfo>();
            await startupServices[1].ApplyYourChangeAsync(serviceProvider);

            //VERIFY
            var authContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            authContext.RoleToPermissions.Count().ShouldEqual(3);
            authContext.UserToRoles.Count().ShouldEqual(5);
        }

        [Fact]
        public void TestSetupAspNetCoreRegisterAuthenticationProviderReader()
        {
            //SETUP
            
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(@"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One")
                .AddAuthUsersIfEmpty(AuthPSetupHelpers.TestUserDefineWithUserId())
                .RegisterAuthenticationProviderReader<StubSyncAuthenticationUsersFactory.StubSyncAuthenticationUsers>()
                .SetupAspNetCoreAndDatabase();

            var serviceProvider = services.BuildServiceProvider();

            //ATTEMPT
            var syncServices = serviceProvider.GetRequiredService<ISyncAuthenticationUsers>();

            //VERIFY
            syncServices.ShouldNotBeNull();
        }

        [Fact]
        public async Task TestSetupAspNetCoreAddSuperUserWithAlteredEntityUser()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(@"Role1 : One, Three
Role2 |my description|: One, Two, Two, Three
Role3: One")
                .AddAuthUsersIfEmpty(AuthPSetupHelpers.TestUserDefineWithSuperUser())
                .RegisterFindUserInfoService<IndividualAccountUserLookup>()
                .IndividualAccountsAuthentication()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase();

            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //ATTEMPT
            startupServices.Count.ShouldEqual(3);
            startupServices[1].ShouldBeType<StartupServiceIndividualAccountsAddSuperUser<IdentityUser>>();
            await startupServices[1].ApplyYourChangeAsync(serviceProvider);
            startupServices[2].ShouldBeType<StartupServiceBulkLoadAuthPInfo>();
            await startupServices[2].ApplyYourChangeAsync(serviceProvider);

            //VERIFY
            var authContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            authContext.RoleToPermissions.Count().ShouldEqual(3);
            authContext.UserToRoles.Count().ShouldEqual(5);
            var superUser = authContext.AuthUsers.First(x => x.UserName == "Super@g1.com");
            superUser.UserId.Length.ShouldBeInRange(25,40);
            using var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            userManager.Users.Count().ShouldEqual(1);
        }

        [Fact]
        public async Task TestSetupAspNetCoreAddRolesPermissionsUsersIfEmptyAndTenants()
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
                .AddAuthUsersIfEmpty(AuthPSetupHelpers.TestUserDefineWithTenants())
                .SetupAspNetCoreAndDatabase();

            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //ATTEMPT
            startupServices.Count.ShouldEqual(2);
            startupServices[1].ShouldBeType<StartupServiceBulkLoadAuthPInfo>();
            await startupServices[1].ApplyYourChangeAsync(serviceProvider);

            //VERIFY
            var authContext = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            foreach (var userToRole in authContext.UserToRoles.ToList())
            {
                _output.WriteLine(userToRole.ToString());
            }
            authContext.AuthUsers.Count().ShouldEqual(3);
            authContext.RoleToPermissions.Count().ShouldEqual(3);
            authContext.UserToRoles.Count().ShouldEqual(5);
            authContext.Tenants.Count().ShouldEqual(3);
        }
    }
}