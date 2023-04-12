// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using Microsoft.Extensions.DependencyInjection;
using Test.StubClasses;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamples
{
    public class UnitTestExample
    {

        [Fact]
        public async Task ExampleUseOfSetupForUnitTestingAsyncForUnitTesting()
        {
            //SETUP
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

            var adminUserService = serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";

            //ATTEMPT
            var status = await adminUserService.FindAuthUserByUserIdAsync(userId);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var rereadUser = status.Result;
            rereadUser.Email.ShouldEqual(userId.ToLower());
            rereadUser.UserName.ShouldEqual(userId);
            rereadUser.UserRoles.OrderBy(x => x.RoleName)
                .Select(x => x.RoleName).ShouldEqual(new List<string> { "Area Manager", "Tenant Admin" });
            rereadUser.UserTenant.TenantFullName.ShouldEqual("4U Inc.");
        }

        [Fact]
        public async Task Example4RolesMarkedAsHidden()
        {
            //SETUP
            var services = new ServiceCollection();
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

            //ATTEMPT
            using var context = serviceProvider.GetRequiredService<AuthPermissionsDbContext>();
            var roles = context.RoleToPermissions.ToList();

            //VERIFY
            roles.Count.ShouldEqual(7);
            roles.Where(x => x.RoleType == RoleTypes.HiddenFromTenant)
                .OrderBy(x => x.RoleName)
                .Select(x => x.RoleName).ShouldEqual(new[] { "App Admin", "SuperAdmin" });
            roles.Where(x => x.RoleType == RoleTypes.Normal)
                .OrderBy(x => x.RoleName)
                .Select(x => x.RoleName)
                .ShouldEqual(new[] { "Area Manager", "Sales Assistant", "Store Manager", "Tenant Admin", "Tenant Director" });
        }
    }
}