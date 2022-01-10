// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore.AccessTenantData;
using AuthPermissions.AspNetCore.AccessTenantData.Services;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Microsoft.Extensions.Options;
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
        var authUser = AuthUser.CreateAuthUser("user1", "user1@g.com", null, new List<RoleToPermissions>()).Result;
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
        var service = new LinkToTenantDataService(context, authOptions, cookieStub, encyptor);

        //ATTEMPT
        await service.StartLinkingToTenantDataAsync(authUser.UserId, tenantIds[1]);

        //VERIFY
        _output.WriteLine($"encrypted string = {cookieStub.CookieValue}");
        service.GetDataKeyOfLinkedTenant().ShouldEqual("2.");
        service.GetNameOfLinkedTenant().ShouldEqual("Tenant2");
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
        var authUser = AuthUser.CreateAuthUser("user1", "user1@g.com", null, new List<RoleToPermissions>()).Result;
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
        var service = new LinkToTenantDataService(context, authOptions, cookieStub, encyptor);

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
        var authUser = AuthUser.CreateAuthUser("user1", "user1@g.com", null, new List<RoleToPermissions>(), tenantToLinkTo).Result;
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
        var service = new LinkToTenantDataService(context, authOptions, cookieStub, encyptor);

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
        var service = new LinkToTenantDataService(context, authOptions, cookieStub, encyptor);

        //ATTEMPT
        cookieStub.CookieValue = null;

        //VERIFY
        service.GetDataKeyOfLinkedTenant().ShouldBeNull();
        service.GetNameOfLinkedTenant().ShouldBeNull();
    }

}