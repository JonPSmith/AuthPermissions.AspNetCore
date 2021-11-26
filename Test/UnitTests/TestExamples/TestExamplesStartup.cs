// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.SetupCode;
using Example1.RazorPages.IndividualAccounts.PermissionsCode;
using Example3.InvoiceCode.EfCoreCode;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.Extensions.DependencyInjection;
using Test.TestHelpers;
using TestSupport.Helpers;
using Xunit;
using Xunit.Abstractions;

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
                .AddRolesPermissionsIfEmpty(AppAuthSetupData.ListOfRolesWithPermissions)
                .AddAuthUsersIfEmpty(AppAuthSetupData.UsersRolesDefinition)
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
                    options.AppConnectionString = connectionString;
                    options.PathToFolderToLock = TestData.GetTestDataDir();
                })
                //NOTE: This uses the same database as the individual accounts DB
                .UsingEfCoreSqlServer(connectionString)
                .IndividualAccountsAuthentication()
                .RegisterTenantChangeService<InvoiceTenantChangeService>()
                .AddRolesPermissionsIfEmpty(Example3AppAuthSetupData.BulkLoadRolesWithPermissions)
                .AddTenantsIfEmpty(Example3AppAuthSetupData.BulkSingleTenants)
                .AddAuthUsersIfEmpty(Example3AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<IndividualAccountUserLookup>()
                .RegisterAuthenticationProviderReader<SyncIndividualAccountUsers>()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase();

            //VERIFY
        }


    }
}