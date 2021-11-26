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
            startupServices.Count.ShouldEqual(0);
        }

        [Fact]
        public void TestSetupAspNetCoreIndividualAccountsAddSuperUser()
        {
            //SETUP
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .IndividualAccountsAuthentication()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase();

            //ATTEMPT
            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //VERIFY
            startupServices.Count.ShouldEqual(1);
            startupServices.First().ShouldBeType<StartupServiceIndividualAccountsAddSuperUser<IdentityUser>>();
        }

        [Fact]
        public void TestSetupAspNetCoreIndividualAccountsAddSuperUser_CustomIdentityUser()
        {
            //SETUP
            var services = this.SetupServicesForTestCustomIdentityUser();
            services.RegisterAuthPermissions<TestEnum>()
                .UsingInMemoryDatabase()
                .IndividualAccountsAuthentication<CustomIdentityUser>()
                .AddSuperUserToIndividualAccounts<CustomIdentityUser>()
                .SetupAspNetCoreAndDatabase();

            //ATTEMPT
            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //VERIFY
            startupServices.Count.ShouldEqual(1);
            startupServices.First().ShouldBeType<StartupServiceIndividualAccountsAddSuperUser<CustomIdentityUser>>();
        }

        [Fact]
        public void TestSetupAspNetCoreSetupAuthDatabaseOnStartup()
        {
            //SETUP
            var aspNetConnectionString = this.GetUniqueDatabaseConnectionString();
            var services = this.SetupServicesForTest();
            services.RegisterAuthPermissions<TestEnum>(options =>
            {
                options.PathToFolderToLock = TestData.GetTestDataDir();
            })
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
            startupServices.Count.ShouldEqual(2);
            startupServices[0].ShouldBeType<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
            startupServices[1].ShouldBeType<StartupServiceMigrateAuthPDatabase>();
        }

        [Fact]
        public void TestSetupAspNetCoreAddRolesPermissionsUsersIfEmpty()
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

            //ATTEMPT
            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //VERIFY
            startupServices.Count.ShouldEqual(1);
            startupServices.Single().ShouldBeType<StartupServiceBulkLoadAuthPInfo>();
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
        public void TestSetupAspNetCoreAddSuperUserWithAlteredEntityUser()
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

            //ATTEMPT
            var serviceProvider = services.BuildServiceProvider();
            var startupServices = serviceProvider.GetServices<IStartupServiceToRunSequentially>().OrderBy(x => x.OrderNum).ToList();

            //VERIFY
            startupServices.Count.ShouldEqual(2);
            startupServices[0].ShouldBeType<StartupServiceIndividualAccountsAddSuperUser<IdentityUser>>();
            startupServices[1].ShouldBeType<StartupServiceBulkLoadAuthPInfo>();
        }
    }
}