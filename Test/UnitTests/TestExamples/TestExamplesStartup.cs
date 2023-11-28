// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.AspNetCore.StartupServices;
using AuthPermissions.SetupCode;
using Example1.RazorPages.IndividualAccounts.PermissionsCode;
using Example3.InvoiceCode.AppStart;
using Example3.InvoiceCode.EfCoreCode;
using Example3.MvcWebApp.IndividualAccounts.Data;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RunMethodsSequentially;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.SetupCode;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;
using Test.StubClasses;

namespace Test.UnitTests.TestExamples
{
    public class TestExamplesStartup
    {
        private readonly ITestOutputHelper _output;

        public TestExamplesStartup(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void TestExample1StartUpWorks()
        {
            //SETUP
            var services = new ServiceCollection();

            //ATTEMPT
            services.RegisterAuthPermissions<Example1Permissions>()
                .UsingInMemoryDatabase()
                .IndividualAccountsAuthentication()
                .AddRolesPermissionsIfEmpty(AppAuthSetupData.RolesDefinition)
                .AddAuthUsersIfEmpty(AppAuthSetupData.UsersWithRolesDefinition)
                .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase();

            //VERIFY
        }

        [Fact]
        public void TestExample3StartUpWorks()
        {
            //SETUP
            var connectionString = this.GetUniqueDatabaseConnectionString();
            var services = new ServiceCollection();

            //ATTEMPT
            services.RegisterAuthPermissions<Example3Permissions>(options =>
                {
                    options.TenantType = TenantTypes.HierarchicalTenant;
                    options.PathToFolderToLock = TestData.GetTestDataDir();
                })
                //NOTE: This uses the same database as the individual accounts DB
                .UsingEfCoreSqlServer(connectionString)
                .IndividualAccountsAuthentication()
                .RegisterTenantChangeService<InvoiceTenantChangeService>()
                .AddRolesPermissionsIfEmpty(Example3AppAuthSetupData.RolesDefinition)
                .AddTenantsIfEmpty(Example3AppAuthSetupData.TenantDefinition)
                .AddAuthUsersIfEmpty(Example3AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<IndividualAccountUserLookup>()
                .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase();

            //VERIFY
        }

        [Fact]
        public async Task TestExample3RunMethodsSequentiallyAsync()
        {
            //SETUP
            var connectionString = this.GetUniqueDatabaseConnectionString();

            var builder = new RegisterRunMethodsSequentiallyTester();

            //Register individual accounts
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            //Wanted to use the line below but just couldn't get the right package for it
            //services.AddDefaultIdentity<IdentityUser>()
            builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            //Have to manually add IHttpContextAccessor (ASP.NET Core adds this by default)
            builder.Services.AddHttpContextAccessor();

            //Have to manually add configuration, using a copy of the Example3 appsettings.json file (ASP.NET Core adds this by default)
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(TestData.GetTestDataDir())
                .AddJsonFile("example3-appsettings.json", optional: false);
            builder.Services.AddSingleton<IConfiguration>(configBuilder.Build());

            //Regsiter the Example3 invoice DbContext
            builder.Services.AddDbContext<InvoicesDbContext>(options =>
                options.UseSqlServer(connectionString, dbOptions =>
                dbOptions.MigrationsHistoryTable("SomeNonDefaultHistoryName")));

            //ATTEMPT
            builder.Services.RegisterAuthPermissions<Example3Permissions>(options =>
            {
                options.TenantType = TenantTypes.HierarchicalTenant;
                options.PathToFolderToLock = builder.LockFolderPath;
            })
                //NOTE: This uses the same database as the individual accounts DB
                .UsingEfCoreSqlServer(connectionString)
                .IndividualAccountsAuthentication()
                .RegisterTenantChangeService<InvoiceTenantChangeService>()
                .AddRolesPermissionsIfEmpty(Example3AppAuthSetupData.RolesDefinition)
                .AddTenantsIfEmpty(Example3AppAuthSetupData.TenantDefinition)
                .AddAuthUsersIfEmpty(Example3AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<IndividualAccountUserLookup>()
                .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase(options =>
                {
                    //Migrate individual account database
                    options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<ApplicationDbContext>>();
                    //Add demo users to the database
                    options.RegisterServiceToRunInJob<StartupServicesIndividualAccountsAddDemoUsers>();

                    //Migrate the application part of the database
                    options.RegisterServiceToRunInJob<StartupServiceMigrateAnyDbContext<InvoicesDbContext>>();
                    //This seeds the invoice database (if empty)
                    options.RegisterServiceToRunInJob<StartupServiceSeedInvoiceDbContext>();
                });

            //VERIFY
            await builder.RunHostStartupCodeAsync();
            //I could access the database to check it updated things, but I do that elsewhere
        }
    }
}