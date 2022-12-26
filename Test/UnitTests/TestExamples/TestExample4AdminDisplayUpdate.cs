// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Example6.MvcWebApp.Sharding.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Test.StubClasses;
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
            services.AddLogging();
            var serviceProvider = await services.RegisterAuthPermissions<Example4Permissions>(options =>
                {
                    options.TenantType = TenantTypes.HierarchicalTenant;
                })
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(Example4AppAuthSetupData.RolesDefinition)
                .AddTenantsIfEmpty(Example4AppAuthSetupData.TenantDefinition)
                .AddAuthUsersIfEmpty(Example4AppAuthSetupData.UsersRolesDefinition)
                .RegisterFindUserInfoService<StubIFindUserInfoFactory.StubIFindUserInfo>()
                .SetupForUnitTestingAsync();

            var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();

            return (context, serviceProvider);
        }

        private async Task<(AuthPermissionsDbContext context, ServiceProvider serviceProvider)> SetupExample6DataAsync()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var serviceProvider = await services.RegisterAuthPermissions<Example6Permissions>(options =>
                {
                    options.TenantType = TenantTypes.SingleLevel | TenantTypes.AddSharding;
                    options.Configuration = new ConfigurationManager();
                })
                .UsingInMemoryDatabase()
                .AddRolesPermissionsIfEmpty(Example6AppAuthSetupData.RolesDefinition)
                .AddTenantsIfEmpty(Example6AppAuthSetupData.TenantDefinition)
                .AddAuthUsersIfEmpty(Example6AppAuthSetupData.UsersRolesDefinition)
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
            var dataKey = "1.3."; // 4U Inc. | West Coast
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
        public async Task TestExample4AuthUserUpdatePrepareForUpdateAsync()
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";

            //ATTEMPT
            var status = await SetupManualUserChange.PrepareForUpdateAsync(userId, adminUserService);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            status.Result.Email.ShouldEqual(userId.ToLower());
            status.Result.UserName.ShouldEqual(userId);
            status.Result.RoleNames.OrderBy(x => x).ToList().ShouldEqual(new List<string> { "Area Manager", "Tenant Admin" });
            status.Result.TenantName.ShouldEqual("4U Inc.");

            status.Result.AllRoleNames.Count.ShouldEqual(6);
        }

        [Theory]
        [InlineData("Super@g1.com", 8)]
        [InlineData("admin@4uInc.com", 6)]
        public async Task TestExample4AuthUserUpdatePrepareForUpdateAsyncAllRoleNames(string userId, int numRoles)
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();

            //ATTEMPT
            var status = await SetupManualUserChange.PrepareForUpdateAsync(userId, adminUserService);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            foreach (var roleName in status.Result.AllRoleNames)
            {
                _output.WriteLine(roleName);
            }
            status.Result.AllRoleNames.Count.ShouldEqual(numRoles);
        }

        [Theory]
        [InlineData(true, 10)]
        [InlineData(false, 18)]
        public async Task TestExample4QueryAuthUsers_DataKey(bool useDataKey, int numUsersExpected)
        {
            //SETUP
            var cAnds = await SetupExample4DataAsync();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";
            var dataKey = useDataKey 
                ? (await adminUserService.FindAuthUserByUserIdAsync(userId)).Result.UserTenant.GetTenantDataKey()
                : null;

            //ATTEMPT
            var results = adminUserService.QueryAuthUsers(dataKey).ToList()
                .Select(x => new { x.Email, DataKey = x.UserTenant?.GetTenantDataKey() ?? "- admin user -"} ).ToList();

            //VERIFY
            foreach (var result in results)
            {
                _output.WriteLine($"{result.Email}, {result.DataKey}");
            }
            results.Count.ShouldEqual(numUsersExpected);
        }

        [Theory]
        [InlineData("admin@4uInc.com", 3)]
        [InlineData("user1@Pets.com", 2)]
        [InlineData("user1@BigR.com", 1)]
        public async Task TestExample6QueryAuthUsers_Sharding(string userId, int numUsersExpected)
        {
            //SETUP
            var cAnds = await SetupExample6DataAsync();
            //Move some tenants to another database
            cAnds.context.Tenants.Single(x => x.TenantFullName == "Pets Ltd.").UpdateShardingState("Shard1", true);
            cAnds.context.SaveChanges();

            var adminUserService = cAnds.serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var dataKey = (await adminUserService.FindAuthUserByUserIdAsync(userId)).Result.UserTenant.GetTenantDataKey();
            var shardingKey = (await adminUserService.FindAuthUserByUserIdAsync(userId)).Result.UserTenant.DatabaseInfoName;

            //ATTEMPT
            var results = adminUserService.QueryAuthUsers(dataKey, shardingKey).ToList()
                .Select(x => new
                {
                    x.Email, 
                    DataKey = x.UserTenant?.GetTenantDataKey(),
                    ShardingKey = x.UserTenant?.DatabaseInfoName
                }).ToList();

            //VERIFY
            foreach (var result in results)
            {
                _output.WriteLine($"{result.Email}, {result.DataKey}, {result.ShardingKey}");
            }
            results.Count.ShouldEqual(numUsersExpected);
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
            var status = await authUserUpdate.ChangeAuthUserFromDataAsync(adminUserService, 
                "en".SetupAuthPLoggingLocalizer());

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            cAnds.context.ChangeTracker.Clear();
            var rereadUser = (await adminUserService.FindAuthUserByUserIdAsync(userId)).Result;
            rereadUser.Email.ShouldEqual(userId.ToLower());
            rereadUser.UserName.ShouldEqual(userId);
            rereadUser.UserRoles.Select(x => x.RoleName).ShouldEqual(new List<string> { "Area Manager", "Tenant Admin" });
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
            authUserUpdate.RoleNames = new List<string> {"Area Manager", "Tenant Admin" };
            var status = await authUserUpdate.ChangeAuthUserFromDataAsync(adminUserService,
                "en".SetupAuthPLoggingLocalizer());

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            cAnds.context.ChangeTracker.Clear();
            var rereadUser = (await adminUserService.FindAuthUserByUserIdAsync(userId)).Result;
            rereadUser.Email.ShouldEqual(userId.ToLower());
            rereadUser.UserName.ShouldEqual(userId);
            rereadUser.UserRoles.Select(x => x.RoleName).ShouldEqual(new List<string> { "Area Manager", "Tenant Admin" });
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
            var status = await authUserUpdate.ChangeAuthUserFromDataAsync(adminUserService,
                "en".SetupAuthPLoggingLocalizer());

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            cAnds.context.ChangeTracker.Clear();
            var rereadUser = (await adminUserService.FindAuthUserByUserIdAsync("newuser@gmail.com")).Result;
            rereadUser.Email.ShouldEqual("newuser@gmail.com");
            rereadUser.UserName.ShouldEqual("newuser@gmail.com");
            rereadUser.UserRoles.Select(x => x.RoleName).ShouldEqual(new List<string> { "Area Manager", "Tenant Admin" });
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
            var status = await authUserUpdate.ChangeAuthUserFromDataAsync(adminUserService,
                "en".SetupAuthPLoggingLocalizer());

            //VERIFY
            status.IsValid.ShouldBeFalse();
            status.GetAllErrors().ShouldEqual("A tenant with the name 'Bad tenant name' wasn't found.");
        }


    }
}