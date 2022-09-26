// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.DataLayer;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.SupportCode.DownStatusCode;
using Microsoft.EntityFrameworkCore;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSupportCode;

public class TestTenantKeyOrShardChangeService
{
    [Fact]
    public void TestDetectTenantDataKeyChangeService_NoChange()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var globalAccessor = new StubGlobalChangeTimeService();
        var service = new TenantKeyOrShardChangeService(globalAccessor);
        var context = new AuthPermissionsDbContext(options, new List<IDatabaseStateChangeEvent> { service});

        context.Database.EnsureCreated();

        //ATTEMPT
        context.Add(new RoleToPermissions("name", null, "ab"));
        context.SaveChanges();

        //VERIFY
        globalAccessor.NumTimesCalled.ShouldEqual(0);
    }

    [Fact]
    public async Task TestDetectTenantDataKeyChangeService_DataKeyChanges()
    {
        //SETUP

        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var globalAccessor = new StubGlobalChangeTimeService();
        var service = new TenantKeyOrShardChangeService(globalAccessor);
        var context = new AuthPermissionsDbContext(options, new List<IDatabaseStateChangeEvent> { service});
        context.Database.EnsureCreated();

        await context.BulkLoadHierarchicalTenantInDbAsync();
        globalAccessor.NumTimesCalled.ShouldEqual(0);

        //ATTEMPT
        var shop1 = context.Tenants
            .Include(x => x.Children)
            .Single(x => x.TenantFullName.EndsWith("Shop1"));
        var newYork = context.Tenants.Single(x => x.TenantFullName.EndsWith("New York"));
        shop1.MoveTenantToNewParent(newYork, x => { });
        context.SaveChanges();

        //VERIFY
        globalAccessor.NumTimesCalled.ShouldEqual(1);
    }


    [Fact]
    public async Task TestDetectTenantDataKeyChangeService_ShardingChanges()
    {
        //SETUP

        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var globalAccessor = new StubGlobalChangeTimeService();
        var service = new TenantKeyOrShardChangeService(globalAccessor);
        var context = new AuthPermissionsDbContext(options, new List<IDatabaseStateChangeEvent> { service});
        context.Database.EnsureCreated();

        await context.SetupSingleShardingTenantsInDbAsync();
        globalAccessor.NumTimesCalled.ShouldEqual(0);

        //ATTEMPT
        var tenant = context.Tenants
            .Single(x => x.TenantFullName == "Tenant2");
        tenant.UpdateShardingState("NewDatabase", true);
        context.SaveChanges();

        //VERIFY
        globalAccessor.NumTimesCalled.ShouldEqual(1);
    }
}