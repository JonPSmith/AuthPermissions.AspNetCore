// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.OpenIdCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.AddUsersServices;
using AuthPermissions.SupportCode.AddUsersServices.Authentication;
using Microsoft.Extensions.Options;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSupportCode;

public class TestAzureAdUserManager
{
    private readonly ITestOutputHelper _output;

    public TestAzureAdUserManager(ITestOutputHelper output)
    {
        _output = output;
    }

    private static async Task<(AzureAdNewUserManager service, AuthUsersAdminService userAdmin)>
        CreateAzureAdUserManagerAsync(AuthPermissionsDbContext context,
            TenantTypes tenantType = TenantTypes.NotUsingTenants)
    {
        var authOptions = new AuthPermissionsOptions
        {
            TenantType = tenantType
        };
        var userAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(), 
            authOptions, "en".SetupAuthPLoggingLocalizer());
        var tenantAdmin = new AuthTenantAdminService(context, authOptions, 
            "en".SetupAuthPLoggingLocalizer(), new StubTenantChangeServiceFactory(), null);
        var azureAdStub = new StubAzureAdAccessService();
        var azureOptions = Options.Create(new AzureAdOptions{ AzureAdApproaches = "Find,Create"});

        var service = new AzureAdNewUserManager(userAdmin, tenantAdmin, azureAdStub, azureOptions,
            "en".SetupAuthPLoggingLocalizer());

        if (tenantType == TenantTypes.SingleLevel)
            context.Add(AuthPSetupHelpers.CreateTestSingleTenantOk("Company"));
        else if (tenantType == TenantTypes.HierarchicalTenant)
            await context.BulkLoadHierarchicalTenantInDbAsync();
        context.SaveChanges();

        context.AddOneUserWithRolesAndOptionalTenant("User1@g.com",
            tenantType != TenantTypes.NotUsingTenants ? "Company" : null);
        return (service, userAdmin);
    }

    [Theory]
    [InlineData("User1@g.com", false)]
    [InlineData("NewUser@g.com", true)]
    public async Task TestCheckNoExistingAuthUserAsync_Email(string email, bool isValid)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateAzureAdUserManagerAsync(context);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var userData = new AddNewUserDto { Email = email };
        var status = await tuple.service.CheckNoExistingAuthUserAsync(userData);

        //VERIFY
        status.IsValid.ShouldEqual(isValid);
    }

    [Fact]
    public async Task TestSetUserInfoAsyncOk()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateAzureAdUserManagerAsync(context);
        var userData = new AddNewUserDto
        {
            Email = "me@gmail.com",
            Roles = new() { "Role1", "Role2" }
        };

        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await tuple.service.SetUserInfoAsync(userData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        var user = (await tuple.userAdmin.FindAuthUserByEmailAsync(userData.Email)).Result;
        user.UserId.ShouldEqual("Azure-AD-userId");
        user.UserRoles.Select(x => x.RoleName).ShouldEqual(new[] { "Role1", "Role2" });
        _output.WriteLine($"Password = {tuple.service.UserLoginData.Password}");
    }
}