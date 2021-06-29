// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Example4.MvcWebApp.IndividualAccounts.Models;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.Extensions.DependencyInjection;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class TestExample4AdminDisplayUpdate
    {
        private readonly ITestOutputHelper _output;
        
        public TestExample4AdminDisplayUpdate(ITestOutputHelper output)
        {
            _output = output;
        }

        private async Task<(AuthPermissionsDbContext context, ServiceProvider serviceProvider)> SetupExample4DataAsync()
        {
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

            return (context, services.BuildServiceProvider());
        }

        [Fact]
        public async Task TestExample4Setup()
        {
            //SETUP

            //ATTEMPT
            var cAnds = await SetupExample4DataAsync();

            //VERIFY
            cAnds.context.ChangeTracker.Clear();
            cAnds.context.AuthUsers.Count().ShouldBeInRange(15,30);
            cAnds.context.RoleToPermissions.Count().ShouldBeInRange(4, 15);
            cAnds.context.UserToRoles.Count().ShouldBeInRange(20, 40);
            cAnds.context.Tenants.Count().ShouldBeInRange(10, 30);
            cAnds.context.AuthUsers.Count(x => x.TenantId == null).ShouldEqual(2);
        }

        [Fact]
        public async Task TestExample4FilterBy()
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            //ATTEMPT
            var dataKey = ".2.5";
            var userQuery = cAnds.context.AuthUsers.Where(x => (x.UserTenant.ParentDataKey+x.TenantId).StartsWith(dataKey));
            var usersToShow = userQuery.ToList();
            var allUsers = cAnds.context.AuthUsers.ToList();

            //VERIFY
            usersToShow.Count.ShouldEqual(6);
            allUsers.Count.ShouldEqual(18);
        }

        [Fact]
        public async Task TestExample4AuthUserUpdateBuildAuthUserUpdateAsync()
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";

            //ATTEMPT
            var status = await AuthUserUpdate.BuildAuthUserUpdateAsync(userId, adminUserService, cAnds.context);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.Email.ShouldEqual(userId);
            status.Result.UserName.ShouldEqual(userId);
            status.Result.RoleNames.ShouldEqual(new List<string>{ "Store Manager", "Tenant Admin" });
            status.Result.TenantName.ShouldEqual("4U Inc.");

            status.Result.AllRoleNames.Count.ShouldEqual(7);
        }

        [Fact]
        public async Task TestExample4AuthUserUpdateUpdateAuthUserFromDataAsyncAllOkNoChange()
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";
            var authUserUpdate = (await AuthUserUpdate.BuildAuthUserUpdateAsync(userId, adminUserService, cAnds.context)).Result;

            //ATTEMPT
            var status = await authUserUpdate.UpdateAuthUserFromDataAsync(adminUserService, cAnds.context);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            cAnds.context.ChangeTracker.Clear();
            var rereadUser = await adminUserService.FindAuthUserByUserIdAsync(userId);
            rereadUser.Email.ShouldEqual(userId);
            rereadUser.UserName.ShouldEqual(userId);
            rereadUser.UserRoles.Select(x => x.RoleName).ShouldEqual(new List<string> { "Store Manager", "Tenant Admin" });
            rereadUser.UserTenant.TenantName.ShouldEqual("4U Inc.");
        }




    }
}