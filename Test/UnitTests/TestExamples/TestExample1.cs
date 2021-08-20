// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.AspNetCore.Services;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Example1.RazorPages.IndividualAccounts.PermissionsCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.Extensions.DependencyInjection;
using Test.TestHelpers;
using TestSupport.Attributes;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class TestExample1
    {
        private readonly ITestOutputHelper _output;

        public TestExample1(ITestOutputHelper output)
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
                .AddRolesPermissionsIfEmpty(AppAuthSetupData.ListOfRolesWithPermissions)
                .AddAuthUsersIfEmpty(AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
                .AddSuperUserToIndividualAccounts()
                .SetupAspNetCoreAndDatabase();

            //VERIFY
        }


    }
}