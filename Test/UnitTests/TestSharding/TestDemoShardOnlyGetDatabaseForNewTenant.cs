// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.SupportCode;
using Test.StubClasses;
using Test.TestHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSharding;

public class TestDemoShardOnlyGetDatabaseForNewTenant
{
    [Fact]
    public async Task TestFindOrCreateDatabaseAsync()
    {
        //SETUP
        var getSetSharding = new StubGetSetShardingEntries(this);
        var tenantChangeServiceFact = new StubTenantChangeServiceFactory();
        var service = new DemoShardOnlyGetDatabaseForNewTenant(getSetSharding, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var status = await service.FindOrCreateShardingEntryAsync(true, "timestamp", "SouthDb");

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
}