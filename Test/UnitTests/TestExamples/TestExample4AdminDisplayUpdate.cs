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
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;
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
            var serviceProvider = await services.RegisterAuthPermissions<Example4Permissions>(options =>
                {
                    options.TenantType = TenantTypes.HierarchicalTenant;
                })
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(Example4AppAuthSetupData.RolesDefinition)
                .AddTenantsIfEmpty(Example4AppAuthSetupData.BulkHierarchicalTenants)
                .AddAuthUsersIfEmpty(Example4AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
                .SetupForUnitTestingAsync();

            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            return (context, serviceProvider);
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
            var dataKey = ".1.3"; // 4U Inc. | West Coast
            var userQuery = cAnds.context.AuthUsers.Where(x => (x.UserTenant.ParentDataKey+x.TenantId).StartsWith(dataKey));
            var usersToShow = userQuery.ToList();
            var allUsers = cAnds.context.AuthUsers.ToList();

            //VERIFY
            foreach (var item in cAnds.context.Tenants)
            {
                _output.WriteLine(item.ToString());
            }
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
            var status = await SetupManualUserChange.PrepareForUpdateAsync(userId, adminUserService);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.Email.ShouldEqual(userId);
            status.Result.UserName.ShouldEqual(userId);
            status.Result.RoleNames.OrderBy(x => x).ToList().ShouldEqual(new List<string>{ "Store Manager", "Tenant Admin" });
            status.Result.TenantName.ShouldEqual("4U Inc.");

            status.Result.AllRoleNames.Count.ShouldEqual(7);
        }

        [Fact]
        public async Task TestExample4QueryAuthUsers()
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";
            var dataKey = (await adminUserService.FindAuthUserByUserIdAsync(userId)).Result.UserTenant.GetTenantDataKey();

            //ATTEMPT
            var results = adminUserService.QueryAuthUsers(dataKey)
                .Select(x => new { x.Email, DataKey = x.UserTenant.GetTenantDataKey()} ).ToList();

            //VERIFY
            foreach (var result in results)
            {
                _output.WriteLine($"{result.Email}, {result.DataKey}");
            }
            results.Count.ShouldEqual(10);
        }

        [Fact]
        public async Task TestExample4AuthUserUpdateChangeAuthUserFromDataAsyncAllOkNoChange()
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";
            var authUserUpdate = (await SetupManualUserChange.PrepareForUpdateAsync(userId, adminUserService)).Result;

            //ATTEMPT
            var status = await authUserUpdate.ChangeAuthUserFromDataAsync(adminUserService);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            cAnds.context.ChangeTracker.Clear();
            var rereadUser = (await adminUserService.FindAuthUserByUserIdAsync(userId)).Result;
            rereadUser.Email.ShouldEqual(userId);
            rereadUser.UserName.ShouldEqual(userId);
            rereadUser.UserRoles.Select(x => x.RoleName).ShouldEqual(new List<string> { "Store Manager", "Tenant Admin" });
            rereadUser.UserTenant.TenantFullName.ShouldEqual("4U Inc.");
        }

        [Fact]
        public async Task TestExample4AuthUserUpdateChangeAuthUserFromDataAsyncChangeRoles()
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";
            var authUserUpdate = (await SetupManualUserChange.PrepareForUpdateAsync(userId, adminUserService)).Result;

            //ATTEMPT
            authUserUpdate.FoundChangeType = SyncAuthUserChangeTypes.Update;
            authUserUpdate.RoleNames = new List<string> {"Area Manager", "App Admin"};
            var status = await authUserUpdate.ChangeAuthUserFromDataAsync(adminUserService);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            cAnds.context.ChangeTracker.Clear();
            var rereadUser = (await adminUserService.FindAuthUserByUserIdAsync(userId)).Result;
            rereadUser.Email.ShouldEqual(userId);
            rereadUser.UserName.ShouldEqual(userId);
            rereadUser.UserRoles.Select(x => x.RoleName).ShouldEqual(new List<string> { "App Admin", "Area Manager" });
            rereadUser.UserTenant.TenantFullName.ShouldEqual("4U Inc.");
        }

        [Fact]
        public async Task TestExample4AuthUserUpdateChangeAuthUserFromDataAsyncAddNewUser()
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var authUserUpdate = (await SetupManualUserChange.PrepareForUpdateAsync("admin@4uInc.com", adminUserService)).Result;

            //ATTEMPT
            authUserUpdate.FoundChangeType = SyncAuthUserChangeTypes.Create;
            authUserUpdate.UserId = "newuser@gmail.com";
            authUserUpdate.Email = "newuser@gmail.com";
            authUserUpdate.UserName = "newuser@gmail.com";
            var status = await authUserUpdate.ChangeAuthUserFromDataAsync(adminUserService);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            cAnds.context.ChangeTracker.Clear();
            var rereadUser = (await adminUserService.FindAuthUserByUserIdAsync("newuser@gmail.com")).Result;
            rereadUser.Email.ShouldEqual("newuser@gmail.com");
            rereadUser.UserName.ShouldEqual("newuser@gmail.com");
            rereadUser.UserRoles.Select(x => x.RoleName).ShouldEqual(new List<string> { "Store Manager", "Tenant Admin" });
            rereadUser.UserTenant.TenantFullName.ShouldEqual("4U Inc.");
        }

        [Fact]
        public async Task TestExample4AuthUserUpdateChangeAuthUserFromDataAsyncBadTenantName()
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";
            var authUserUpdate = (await SetupManualUserChange.PrepareForUpdateAsync(userId, adminUserService)).Result;

            //ATTEMPT
            authUserUpdate.FoundChangeType = SyncAuthUserChangeTypes.Update;
            authUserUpdate.TenantName = "Bad tenant name";
            var status = await authUserUpdate.ChangeAuthUserFromDataAsync(adminUserService);

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("A tenant with the name 'Bad tenant name' wasn't found.");
        }


    }
}