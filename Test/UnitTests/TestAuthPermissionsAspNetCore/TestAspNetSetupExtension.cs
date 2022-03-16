// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.CommonCode;
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

        private enum EnumNotShort {One, Two}

        [Fact]
        public async Task TestRegisterAuthPermissionsEnumNotShort()
        {
            //SETUP
            var services = this.SetupServicesForTest();

            //ATTEMPT
            var ex = await Assert.ThrowsAsync<AuthPermissionsException>(async () => 
                await services.RegisterAuthPermissions<EnumNotShort>()
                .UsingInMemoryDatabase()
                .SetupForUnitTestingAsync());

            //VERIFY
            ex.Message.ShouldStartWith($"The enum permissions {nameof(EnumNotShort)} should by 16 bits in size to work");
        }

        [Theory]
        [InlineData(TenantTypes.NotUsingTenants, true)]
        [InlineData(TenantTypes.SingleLevel, true)]
        [InlineData(TenantTypes.HierarchicalTenant, true)]
        [InlineData(TenantTypes.SingleLevel | TenantTypes.AddSharding, true)]
        [InlineData(TenantTypes.HierarchicalTenant | TenantTypes.AddSharding, true)]
        [InlineData(TenantTypes.SingleLevel | TenantTypes.HierarchicalTenant, false)]
        [InlineData(TenantTypes.AddSharding, false)]
        public async Task TestRegisterAuthPermissionsMultiTenantChecks(TenantTypes tenantType, bool success)
        {
            //SETUP
            var services = this.SetupServicesForTest();

            //ATTEMPT
            try
            {
                await services.RegisterAuthPermissions<TestEnum>(options => options.TenantType = tenantType)
                    .UsingInMemoryDatabase()
                    .SetupForUnitTestingAsync();
            }
            catch (Exception e)
            {
                _output.WriteLine(e.Message);
                success.ShouldBeFalse();
                return;
            }

            //VERIFY
            success.ShouldBeTrue();
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
                .AddRolesPermissionsIfEmpty(AuthPSetupHelpers.TestRolesDefinition123)
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
                .AddRolesPermissionsIfEmpty(AuthPSetupHelpers.TestRolesDefinition123)
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
                .AddRolesPermissionsIfEmpty(AuthPSetupHelpers.TestRolesDefinition123)
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