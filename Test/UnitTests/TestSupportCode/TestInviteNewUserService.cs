// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.SupportCode.AddUsersServices;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSupportCode;

public class TestInviteNewUserService
{
    private readonly ITestOutputHelper _output;

    public TestInviteNewUserService(ITestOutputHelper output)
    {
        _output = output;
    }

    private static InviteNewUserService CreateInviteAndAddSenderAuthUser(AuthPermissionsDbContext context, out AuthUsersAdminService userAdmin)
    {
        var authOptions = new AuthPermissionsOptions { EncryptionKey = "asfafffggdgerxbd" };
        userAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(), authOptions);
        var encryptService = new EncryptDecryptService(authOptions);
        var service =
            new InviteNewUserService(encryptService, userAdmin, new StubAuthenticationAddUserManager(userAdmin));

        context.AddOneUserWithRoles();
        return service;
    }

    [Fact]
    public async Task TestInviteUserToJoinTenantAsync()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var service = CreateInviteAndAddSenderAuthUser(context, out var userAdmin);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var dto = new AddUserDataDto { Email = "User2@g.com", Roles = new List<string>{"Role1"}};
        var status = await service.CreateInviteUserToJoinAsync(dto, "User1");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _output.WriteLine(status.Result);
    }

    [Theory]
    [InlineData("User2@g.com", true)]
    [InlineData("BadEmail@g.com", false)]
    public async Task TestAddUserViaInvite(string emailGiven, bool isValid)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var service = CreateInviteAndAddSenderAuthUser(context, out var userAdmin);
        var dto = new AddUserDataDto { Email = "User2@g.com", Roles = new List<string> { "Role1", "Role2" } };
        var inviteStatus = await service.CreateInviteUserToJoinAsync(dto, "User1");

        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await service.AddUserViaInvite(inviteStatus.Result, emailGiven);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldEqual(isValid);
        if (isValid)
        {
            var statusNewUser = await userAdmin.FindAuthUserByEmailAsync("User2@g.com");
            statusNewUser.Result.UserRoles.Select(x => x.RoleName).ShouldEqual(new[] { "Role1", "Role2" });
        }
    }


}