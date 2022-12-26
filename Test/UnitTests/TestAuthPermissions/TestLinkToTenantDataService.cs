// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.AccessTenantData.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions;

public class TestLinkToTenantDataService
{
    private readonly ITestOutputHelper _output;

    public TestLinkToTenantDataService(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestStartLinkingToTenantDataAsyncAppUserSingleTenant()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenantIds = context.SetupSingleTenantsInDb();
        var authUser = AuthPSetupHelpers.CreateTestAuthUserOk("user1", "user1@g.com", null);
        context.Add(authUser);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var authOptions = new AuthPermissionsOptions
        {
            TenantType = TenantTypes.SingleLevel,
            LinkToTenantType = LinkToTenantTypes.OnlyAppUsers,
            EncryptionKey = "asfafffggdgerxbd"
        };

        var cookieStub = new StubIAccessTenantDataCookie();
        var encyptor = new EncryptDecryptService(authOptions);
        var service = new LinkToTenantDataService(context, authOptions, cookieStub, encyptor,
            "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var status = await service.StartLinkingToTenantDataAsync(authUser.UserId, tenantIds[1]);

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _output.WriteLine($"encrypted string = {cookieStub.CookieValue}");
        service.GetDataKeyOfLinkedTenant().ShouldEqual("2.");
        service.GetNameOfLinkedTenant().ShouldEqual("Tenant2");
    }

    [Fact]
    public async Task TestStartLinkingToTenantDataAsyncAppUserSharding()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenant = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1");
        tenant.UpdateShardingState("MyConnectionName", true);
        var authUser = AuthPSetupHelpers.CreateTestAuthUserOk("user1", "user1@g.com", null);
        context.AddRange(authUser, tenant);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var authOptions = new AuthPermissionsOptions
        {
            TenantType = TenantTypes.SingleLevel | TenantTypes.AddSharding,
            LinkToTenantType = LinkToTenantTypes.OnlyAppUsers,
            EncryptionKey = "asfafffggdgerxbd"
        };

        var cookieStub = new StubIAccessTenantDataCookie();
        var encyptor = new EncryptDecryptService(authOptions);
        var service = new LinkToTenantDataService(context, authOptions, cookieStub, encyptor,
            "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var status = await service.StartLinkingToTenantDataAsync(authUser.UserId, tenant.TenantId);

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _output.WriteLine($"encrypted string = {cookieStub.CookieValue}");
        service.GetShardingDataOfLinkedTenant().ShouldEqual(("NoQueryFilter", "MyConnectionName"));
        service.GetNameOfLinkedTenant().ShouldEqual("Tenant1");
    }

    [Theory]
    [InlineData(LinkToTenantTypes.NotTurnedOn, true)]
    [InlineData(LinkToTenantTypes.OnlyAppUsers, false)]
    [InlineData(LinkToTenantTypes.AppAndHierarchicalUsers, false)]
    public async Task TestStartLinkingToTenantDataAsyncAppUserCheckingTypes(LinkToTenantTypes toTenantTypes, bool shouldThrow)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenantIds = context.SetupSingleTenantsInDb();
        var authUser = AuthPSetupHelpers.CreateTestAuthUserOk("user1", "user1@g.com", null);
        context.Add(authUser);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var authOptions = new AuthPermissionsOptions
        {
            TenantType = TenantTypes.SingleLevel,
            LinkToTenantType = toTenantTypes,
            EncryptionKey = "asfafffggdgerxbd"
        };

        var cookieStub = new StubIAccessTenantDataCookie();
        var encyptor = new EncryptDecryptService(authOptions);
        var service = new LinkToTenantDataService(context, authOptions, cookieStub, encyptor,
            "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        try
        {
            await service.StartLinkingToTenantDataAsync(authUser.UserId, tenantIds[1]);
        }
        catch (AuthPermissionsException e)
        {
            _output.WriteLine(e.Message);
            shouldThrow.ShouldBeTrue();
            return;
        }

        //VERIFY
        shouldThrow.ShouldBeFalse();
    }

    [Theory]
    [InlineData(LinkToTenantTypes.NotTurnedOn, true)]
    [InlineData(LinkToTenantTypes.OnlyAppUsers, true)]
    [InlineData(LinkToTenantTypes.AppAndHierarchicalUsers, false)]
    public async Task TestStartLinkingToTenantDataAsyncTenantUserCheckingTypes(LinkToTenantTypes toTenantTypes, bool shouldThrow)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenantIds = context.SetupSingleTenantsInDb();
        var tenantToLinkTo = context.Tenants.First();
        var authUser = AuthPSetupHelpers.CreateTestAuthUserOk("user1", "user1@g.com", null, 
            new List<RoleToPermissions>(), tenantToLinkTo);
        context.Add(authUser);
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var authOptions = new AuthPermissionsOptions
        {
            TenantType = TenantTypes.SingleLevel,
            LinkToTenantType = toTenantTypes,
            EncryptionKey = "asfafffggdgerxbd"
        };

        var cookieStub = new StubIAccessTenantDataCookie();
        var encyptor = new EncryptDecryptService(authOptions);
        var service = new LinkToTenantDataService(context, authOptions, cookieStub, encyptor,
            "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        try
        {
            await service.StartLinkingToTenantDataAsync(authUser.UserId, tenantIds[1]);
        }
        catch (AuthPermissionsException e)
        {
            _output.WriteLine(e.Message);
            shouldThrow.ShouldBeTrue();
            return;
        }

        //VERIFY
        shouldThrow.ShouldBeFalse();
    }

    [Fact]
    public void TestGetDataKeyOfLinkedTenantNoCookie()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var authOptions = new AuthPermissionsOptions
        {
            TenantType = TenantTypes.SingleLevel,
            LinkToTenantType = LinkToTenantTypes.OnlyAppUsers,
            EncryptionKey = "asfafffggdgerxbd"
        };

        var cookieStub = new StubIAccessTenantDataCookie();
        var encyptor = new EncryptDecryptService(authOptions);
        var service = new LinkToTenantDataService(context, authOptions, cookieStub, encyptor,
            "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        cookieStub.CookieValue = null;

        //VERIFY
        service.GetDataKeyOfLinkedTenant().ShouldBeNull();
        service.GetNameOfLinkedTenant().ShouldBeNull();
    }
    
}