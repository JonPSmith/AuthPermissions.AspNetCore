// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.SupportCode;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSharding;

public class TestDemoShardOnlyGetDatabaseForNewTenant
{
    [Fact]
    public async Task TestFindOrCreateDatabaseAsync()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();


        var getSetSharding = new StubGetSetShardingEntries(this);
        var tenantChangeServiceFact = new StubTenantChangeServiceFactory();
        var service = new DemoShardOnlyGetDatabaseForNewTenant(getSetSharding,
            context, tenantChangeServiceFact.GetService(true), "en".SetupAuthPLoggingLocalizer());

        var tenant = Tenant.CreateSingleTenant(
            "MyTenant", new StubDefaultLocalizer()).Result;
        context.Add(tenant);
        context.SaveChanges();

        //ATTEMPT
        var status = await service.FindOrCreateDatabaseAsync(tenant, true, "Other Database");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        getSetSharding.CalledMethodName.ShouldEqual("AddNewShardingEntry");
        getSetSharding.SharingEntryAddUpDel.Name.ShouldEqual("Tenant_1");
        getSetSharding.SharingEntryAddUpDel.ConnectionName.ShouldEqual("Other Database");
        getSetSharding.SharingEntryAddUpDel.DatabaseName.ShouldEqual("Tenant_1");
        getSetSharding.SharingEntryAddUpDel.DatabaseType.ShouldEqual("SqlServer");
        tenantChangeServiceFact.CalledMethodName.ShouldEqual("CreateNewTenantAsync");
        tenantChangeServiceFact.NewTenantName.ShouldEqual("MyTenant");
    }

    [Fact]
    public async Task TestRemoveLastDatabaseSetupAsync()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var getSetSharding = new StubGetSetShardingEntries(this);
        var tenantChangeServiceFact = new StubTenantChangeServiceFactory();
        var service = new DemoShardOnlyGetDatabaseForNewTenant(getSetSharding,
            context, tenantChangeServiceFact.GetService(true), "en".SetupAuthPLoggingLocalizer());

        var tenant = Tenant.CreateSingleTenant(
            "MyTenant", new StubDefaultLocalizer()).Result;
        context.Add(tenant);
        context.SaveChanges();

        (await service.FindOrCreateDatabaseAsync(tenant, true, "Other Database")).IsValid.ShouldBeTrue();
        tenantChangeServiceFact.CalledMethodName = null;

        //ATTEMPT
        var status = await service.RemoveLastDatabaseSetupAsync();

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        getSetSharding.CalledMethodName.ShouldEqual("GetSingleShardingEntry");
        getSetSharding.SharingEntryAddUpDel.Name.ShouldEqual("Tenant_1");
        tenantChangeServiceFact.CalledMethodName.ShouldBeNull();
        tenantChangeServiceFact.NewTenantName.ShouldEqual("MyTenant");
    }
}