// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore;
using AuthPermissions.SetupCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class TestAdminDisplay
    {
        private readonly ITestOutputHelper _output;

        public TestAdminDisplay(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task TestExample4Setup()
        {
            //SETUP
            var services = new ServiceCollection();

            //ATTEMPT
            var context = await services.RegisterAuthPermissions<Example4Permissions>(options =>
                {
                    options.TenantType = TenantTypes.HierarchicalTenant;
                })
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(Example4AppAuthSetupData.BulkLoadRolesWithPermissions)
                .AddTenantsIfEmpty(Example4AppAuthSetupData.BulkHierarchicalTenants)
                .AddUsersRolesIfEmpty(Example4AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
                .SetupForUnitTestingAsync();

            //VERIFY
            context.ChangeTracker.Clear();
            context.AuthUsers.Count().ShouldBeInRange(15,30);
            context.RoleToPermissions.Count().ShouldBeInRange(4, 15);
            context.UserToRoles.Count().ShouldBeInRange(20, 40);
            context.Tenants.Count().ShouldBeInRange(10, 30);
            context.AuthUsers.Count(x => x.TenantId == null).ShouldEqual(2);
        }

        [Fact]
        public async Task TestExample4FilterBy()
        {
            //SETUP
            var services = new ServiceCollection();
            var context = await services.RegisterAuthPermissions<Example4Permissions>(options =>
                {
                    options.TenantType = TenantTypes.HierarchicalTenant;
                })
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(Example4AppAuthSetupData.BulkLoadRolesWithPermissions)
                .AddTenantsIfEmpty(Example4AppAuthSetupData.BulkHierarchicalTenants)
                .AddUsersRolesIfEmpty(Example4AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
                .SetupForUnitTestingAsync();

            //ATTEMPT
            var dataKey = ".2.5";
            var userQuery = context.AuthUsers.Where(x => (x.UserTenant.ParentDataKey+x.TenantId).StartsWith(dataKey));
            var usersToShow = userQuery.ToList();
            var allUsers = context.AuthUsers.ToList();

            //VERIFY
            var xx = userQuery.ToQueryString();


        }


    }
}