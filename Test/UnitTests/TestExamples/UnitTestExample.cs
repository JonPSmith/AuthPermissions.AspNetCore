// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AspNetCore;
using AuthPermissions.SetupCode;
using Example4.MvcWebApp.IndividualAccounts.PermissionsCode;
using ExamplesCommonCode.CommonAdmin;
using Microsoft.Extensions.DependencyInjection;
using Test.TestHelpers;
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

            var adminUserService = serviceProvider.GetRequiredService<IAuthUsersAdminService>();
            var userId = "admin@4uInc.com";

            //ATTEMPT
            var status = await adminUserService.FindAuthUserByUserIdAsync(userId);

            //VERIFY
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            var rereadUser = status.Result;
            rereadUser.Email.ShouldEqual(userId);
            rereadUser.UserName.ShouldEqual(userId);
            rereadUser.UserRoles.Select(x => x.RoleName).ShouldEqual(new List<string> { "Tenant Admin", "Store Manager" });
            rereadUser.UserTenant.TenantFullName.ShouldEqual("4U Inc.");
        }
    }
}