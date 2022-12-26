// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SupportCode.AddUsersServices;
using Microsoft.IdentityModel.Tokens;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;
using static Humanizer.In;

namespace Test.UnitTests.TestSupportCode;

public class TestInviteNewUserService
{
    private readonly ITestOutputHelper _output;

    public TestInviteNewUserService(ITestOutputHelper output)
    {
        _output = output;
    }

    private static async Task<(InviteNewUserService service, AuthUsersAdminService userAdmin, EncryptDecryptService encryptService)> 
        CreateInviteAndAddSenderAuthUserAsync(AuthPermissionsDbContext context, 
        TenantTypes tenantType = TenantTypes.NotUsingTenants)
    {
        var authOptions = new AuthPermissionsOptions
        {
            EncryptionKey = "asfafffggdgerxbd", TenantType = tenantType
        };
        var userAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(), 
            authOptions, "en".SetupAuthPLoggingLocalizer());
        var encryptService = new EncryptDecryptService(authOptions);
        var service = new InviteNewUserService(authOptions, context, encryptService, userAdmin, 
                new StubAddNewUserManager(userAdmin), "en".SetupAuthPLoggingLocalizer());

        if (tenantType == TenantTypes.SingleLevel)
            context.Add(AuthPSetupHelpers.CreateTestSingleTenantOk("Company"));
        else if (tenantType == TenantTypes.HierarchicalTenant)
            await context.BulkLoadHierarchicalTenantInDbAsync();
        context.SaveChanges();

        context.AddOneUserWithRolesAndOptionalTenant("User1@g.com", 
            tenantType != TenantTypes.NotUsingTenants ? "Company" : null);
        return (service, userAdmin, encryptService);
    }

    [Fact]
    public async Task TestInviteUserToJoinTenantAsync_NoMultiTenant()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var dto = new AddNewUserDto { Email = "User2@g.com", Roles = new List<string>{"Role1"}};
        var status = await tuple.service.CreateInviteUserToJoinAsync(dto, "User1");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _output.WriteLine(status.Message);
    }

    [Fact]
    public async Task TestInviteUserToJoinTenantAsync_NoRoles()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var dto = new AddNewUserDto { Email = "User2@g.com"};
        var status = await tuple.service.CreateInviteUserToJoinAsync(dto, "User1");

        //VERIFY
        status.IsValid.ShouldBeFalse(status.GetAllErrors());
        status.GetAllErrors().ShouldEqual("You haven't set up any Roles for the invited user. " +
                                          "If you really what the user to have no roles, then select the '< none >' dropdown item.");
    }

    [Fact]
    public async Task TestInviteUserToJoinTenantAsync_NoEmailOrUserName()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var dto = new AddNewUserDto { };
        var status = await tuple.service.CreateInviteUserToJoinAsync(dto, "User1");

        //VERIFY
        status.IsValid.ShouldBeFalse();
        status.GetAllErrors().ShouldEqual("You must provide an email or username for the invitation.");
    }

    [Fact]
    public async Task TestInviteUserToJoinTenantAsync_NoTenantBaseSettings()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var dto = new AddNewUserDto { Email = "User2@g.com", Roles = new List<string> { "Role1", "Role2" } };
        var status = await tuple.service.CreateInviteUserToJoinAsync(dto, "User1");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());

        var decrypted = tuple.encryptService.Decrypt(Base64UrlEncoder.Decode(status.Result));
        var invite = JsonSerializer.Deserialize<AddNewUserDto>(decrypted);
        invite.Email.ShouldEqual("user2@g.com");
        invite.Roles.ShouldEqual(new List<string> { "Role1", "Role2" });
        invite.TimeInviteExpires.ShouldEqual(default);
    }

    [Fact]
    public async Task TestListOfExpirationTimes()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();
        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context);

        var utcNowPlusOneHour = DateTime.UtcNow.AddHours(1);

        //ATTEMPT
        var list = tuple.service.ListOfExpirationTimes();

        //VERIFY
        var oneHourExpire = new DateTime(list[1].Key);
        _output.WriteLine($"Expected value = {utcNowPlusOneHour:O}, Actual value = {oneHourExpire:O}");
        oneHourExpire.Ticks.ShouldBeInRange(oneHourExpire.AddMilliseconds(-100).Ticks, oneHourExpire.AddMilliseconds(+100).Ticks);
        var values = list.Select(x => x.Value).ToList();
        values.Count.ShouldEqual(7);
        values[0].ShouldEqual("Invite is valid forever.");
        values[1].ShouldEqual("Invite is only valid for 1 hour from now.");
        values[2].ShouldEqual("Invite is only valid for 6 hours from now.");
        values[3].ShouldEqual("Invite is only valid for 24 hours from now.");
        values[4].ShouldEqual("Invite is only valid for 3 days from now.");
        values[5].ShouldEqual("Invite is only valid for 7 days from now.");
        values[6].ShouldEqual("Invite is only valid for 20 days from now.");

    }

    [Fact]
    public async Task TestInviteUserToJoinTenantAsync_WithTimeout()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var expiresTicks = DateTime.UtcNow.AddHours(1).Ticks;
        var dto = new AddNewUserDto { Email = "User2@g.com", Roles = new List<string> {"< none >"}, TimeInviteExpires = expiresTicks};
        var status = await tuple.service.CreateInviteUserToJoinAsync(dto, "User1");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());

        var decrypted = tuple.encryptService.Decrypt(Base64UrlEncoder.Decode(status.Result));
        var invite = JsonSerializer.Deserialize<AddNewUserDto>(decrypted);
        invite.Email.ShouldEqual("user2@g.com");
        invite.TimeInviteExpires.ShouldEqual(expiresTicks);
        status.Message.ShouldEndWith($". This invite expires on local time {new DateTime(expiresTicks).ToLocalTime():g}.");
    }

    [Theory]
    [InlineData(TenantTypes.SingleLevel, 999, true, 1)]        //Single tenant: takes the invite's tenant
    [InlineData(TenantTypes.HierarchicalTenant, 1, true, 1)]   //Hierarchical: pick the top
    [InlineData(TenantTypes.HierarchicalTenant, 3, true, 3)]   //Hierarchical: pick a child
    [InlineData(TenantTypes.HierarchicalTenant, null, false, null)] //Hierarchical: no tenant set
    [InlineData(TenantTypes.HierarchicalTenant, 99, false, null)] //Hierarchical: bad tenantId
    public async Task TestInviteUserToJoinTenantAsync_MultiTenant_TenantAdmin(
        TenantTypes tenantType, int? joinerTenantId, bool isValid, int? expectedTenantId)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context, tenantType);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var dto = new AddNewUserDto { Email = "User2@g.com", Roles = new List<string> { "Role1" }, TenantId = joinerTenantId};
        var status = await tuple.service.CreateInviteUserToJoinAsync(dto, "User1");

        //VERIFY
        if (status.HasErrors)
            _output.WriteLine(status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
        if (status.IsValid)
        {
            var decrypted = tuple.encryptService.Decrypt(Base64UrlEncoder.Decode(status.Result));
            var invite = JsonSerializer.Deserialize<AddNewUserDto>(decrypted);
            invite.TenantId.ShouldEqual(expectedTenantId);
        }
    }

    [Theory]
    [InlineData(TenantTypes.SingleLevel, null, true, null)]        //make another app user
    [InlineData(TenantTypes.SingleLevel, 1, true, 1)]           //Single tenant: select
    [InlineData(TenantTypes.HierarchicalTenant, null, true, null)] //make another app user
    [InlineData(TenantTypes.HierarchicalTenant, 1, true, 1)]    //Hierarchical: pick the top
    [InlineData(TenantTypes.HierarchicalTenant, 3, true, 3)]    //Hierarchical: pick a child
    [InlineData(TenantTypes.SingleLevel, 99, false, null)]         //Single tenant: bad tenantId
    [InlineData(TenantTypes.HierarchicalTenant, 99, false, null)]  //Hierarchical: bad tenantId
    public async Task TestInviteUserToJoinTenantAsync_MultiTenant_AppAdmin(
        TenantTypes tenantType, int? joinerTenantId, bool isValid, int? expectedTenantId)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context, tenantType);
        context.Add(AuthPSetupHelpers.CreateTestAuthUserOk("AppAdminId", "AppAdmin@g.com", null));
        context.SaveChanges();

        context.ChangeTracker.Clear();

        //ATTEMPT
        var dto = new AddNewUserDto { Email = "User2@g.com", Roles = new List<string> { "Role1" }, TenantId = joinerTenantId };
        var status = await tuple.service.CreateInviteUserToJoinAsync(dto, "AppAdminId");

        //VERIFY
        if (status.HasErrors)
            _output.WriteLine(status.GetAllErrors());
        status.IsValid.ShouldEqual(isValid);
        if (status.IsValid)
        {
            var decrypted = tuple.encryptService.Decrypt(Base64UrlEncoder.Decode(status.Result));
            var invite = JsonSerializer.Deserialize<AddNewUserDto>(decrypted);
            invite.TenantId.ShouldEqual(expectedTenantId);
            _output.WriteLine(status.Message);
        }
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

        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context);
        var dto = new AddNewUserDto { Email = "User2@g.com", Roles = new List<string> { "Role1", "Role2" } };
        var inviteStatus = await tuple.service.CreateInviteUserToJoinAsync(dto, "User1");

        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await tuple.service.AddUserViaInvite(inviteStatus.Result, emailGiven, null);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldEqual(isValid);
        if (isValid)
        {
            var statusNewUser = await tuple.userAdmin.FindAuthUserByEmailAsync("User2@g.com");
            statusNewUser.Result.UserRoles.Select(x => x.RoleName).ShouldEqual(new[] { "Role1", "Role2" });
        }
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(-1, false)]
    public async Task TestAddUserViaInvite_Timeout(int secondsOffset, bool isValid)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = await CreateInviteAndAddSenderAuthUserAsync(context);
        
        var expiresTicks = DateTime.UtcNow.AddSeconds(secondsOffset).Ticks;
        var dto = new AddNewUserDto { Email = "User2@g.com", Roles = new List<string> { CommonConstants.EmptyItemName }, 
            TimeInviteExpires = expiresTicks };
        var inviteStatus = await tuple.service.CreateInviteUserToJoinAsync(dto, "User1");

        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await tuple.service.AddUserViaInvite(inviteStatus.Result, "user2@g.com", null);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldEqual(isValid);
        if (status.HasErrors)
            status.GetAllErrors().ShouldEqual("The invite has expired. Please contact the person who sent you an invite.");
    }
}