// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissionsAdmin;

public class TestTenantAdminServicesSharding
{
    private readonly AuthPermissionsOptions _authOptionsSingleSharding =
        new() { TenantType = TenantTypes.SingleLevel | TenantTypes.AddSharding };
    private readonly AuthPermissionsOptions _authOptionsHierarchicalSharding =
        new() { TenantType = TenantTypes.HierarchicalTenant | TenantTypes.AddSharding };

    [Fact]
    public async Task TestSetupSingleShardingTenantsInDbOk()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        context.ChangeTracker.Clear();

        //ATTEMPT
        await context.SetupSingleShardingTenantsInDbAsync();

        //VERIFY
        context.ChangeTracker.Clear();
        var tenants = context.Tenants.ToList();
        tenants.Select(x => x.TenantFullName).ToArray().ShouldEqual(new[] { "Tenant1", "Tenant2", "Tenant3" });
        tenants.All(x => !x.HasOwnDb).ShouldBeTrue();
        tenants.All(x => x.DatabaseInfoName == "Default Database").ShouldBeTrue();
    }

    [Fact]
    public async Task TestAddSingleTenantAsyncOk()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        await context.SetupSingleShardingTenantsInDbAsync();
        context.ChangeTracker.Clear();

        var tenantChange = new StubTenantChangeServiceFactory();
        var service = new AuthTenantAdminService(context,
            _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
            tenantChange,  null);

        //ATTEMPT
        var status = await service.AddSingleTenantAsync("Tenant4", null, true,"MyConnectionName");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        tenantChange.NewTenantName.ShouldEqual("Tenant4");
        context.ChangeTracker.Clear();
        var tenants = context.Tenants.ToList();
        tenants.Count.ShouldEqual(4);
        tenants.Last().DatabaseInfoName.ShouldEqual("MyConnectionName");
        tenants.Last().HasOwnDb.ShouldEqual(true);
    }

    [Fact]
    public async Task TestAddSingleTenantAsyncBadSettings()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        await context.SetupSingleShardingTenantsInDbAsync();
        context.ChangeTracker.Clear();

        var tenantChange = new StubTenantChangeServiceFactory();
        var service = new AuthTenantAdminService(context,
            _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
            tenantChange, null);

        //ATTEMPT
        var status = await service.AddSingleTenantAsync("Tenant4");

        //VERIFY
        status.IsValid.ShouldBeFalse();
        status.GetAllErrors().ShouldEqual("The 'hasOwnDb' parameter must be set to true or false when sharding is turned on.");
    }

    [Fact]
    public async Task TestAddSingleTenantAsyncHasOwnDbCheck()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        await context.SetupSingleShardingTenantsInDbAsync();
        context.ChangeTracker.Clear();

        var tenantChange = new StubTenantChangeServiceFactory();
        var service = new AuthTenantAdminService(context,
            _authOptionsSingleSharding, "en".SetupAuthPLoggingLocalizer(),
            tenantChange, null);

        //ATTEMPT
        var status = await service.AddSingleTenantAsync("Tenant4", null, true, "Default Database");

        //VERIFY
        status.IsValid.ShouldBeFalse();
        status.GetAllErrors().ShouldEqual(
            "The hasOwnDb parameter is true, but the sharding database name 'Default Database' already has tenant(s) using that database.");
    }

    [Fact]
    public async Task TestBulkLoadHierarchicalTenantShardingAsync()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        context.ChangeTracker.Clear();

        //ATTEMPT
        await context.BulkLoadHierarchicalTenantShardingAsync();

        //VERIFY
        context.ChangeTracker.Clear();
        var tenants = context.Tenants.ToList();
        tenants.Count.ShouldEqual(9);
        tenants.All(x => !x.HasOwnDb).ShouldBeTrue();
        tenants.All(x => x.DatabaseInfoName == "Default Database").ShouldBeTrue();
    }

    [Fact]
    public async Task TestAddHierarchicalTenantAsyncToExistingParentNoShardingParamsOk()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenantIds = await context.BulkLoadHierarchicalTenantShardingAsync();
        context.ChangeTracker.Clear();

        var tenantChange = new StubTenantChangeServiceFactory();
        var service = new AuthTenantAdminService(context,
            _authOptionsHierarchicalSharding, "en".SetupAuthPLoggingLocalizer(),
            tenantChange, null);

        //ATTEMPT
        var status = await service.AddHierarchicalTenantAsync("New Child", tenantIds[1]);

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        tenantChange.NewTenantName.ShouldEqual("Company | West Coast | New Child");
        context.ChangeTracker.Clear();
        var tenants = context.Tenants.ToList();
        tenants.Count.ShouldEqual(10);
        tenants.All(x => !x.HasOwnDb).ShouldBeTrue();
        tenants.All(x => x.DatabaseInfoName == "Default Database").ShouldBeTrue();
    }

    [Fact]
    public async Task TestAddHierarchicalTenantAsyncNoParentGoodParamsOk()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenantIds = await context.BulkLoadHierarchicalTenantShardingAsync();
        context.ChangeTracker.Clear();

        var tenantChange = new StubTenantChangeServiceFactory();
        var service = new AuthTenantAdminService(context,
            _authOptionsHierarchicalSharding, "en".SetupAuthPLoggingLocalizer(),
            tenantChange, null);

        //ATTEMPT
        var status = await service.AddHierarchicalTenantAsync("New Company", 0, null,
            true, "DiffConnectionName");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        tenantChange.NewTenantName.ShouldEqual("New Company");
        context.ChangeTracker.Clear();
        var tenants = context.Tenants.ToList();
        tenants.Count.ShouldEqual(10);
        tenants.Last().HasOwnDb.ShouldBeTrue();
    }

    [Fact]
    public async Task TestAddHierarchicalTenantAsyncToExistingParentBadParams()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenantIds = await context.BulkLoadHierarchicalTenantShardingAsync();
        context.ChangeTracker.Clear();

        var tenantChange = new StubTenantChangeServiceFactory();
        var service = new AuthTenantAdminService(context,
            _authOptionsHierarchicalSharding, "en".SetupAuthPLoggingLocalizer(),
            tenantChange, null);

        //ATTEMPT
        var status = await service.AddHierarchicalTenantAsync("New Child", tenantIds[1], null, 
            true, "DiffConnectionName");

        //VERIFY
        status.IsValid.ShouldBeFalse();
        status.Errors.Count.ShouldEqual(2);
        status.Errors[0].ToString().ShouldEqual("The hasOwnDb parameter doesn't match the parent's HasOwnDb. Set the hasOwnDb parameter to null to use the parent's HasOwnDb value.");
        status.Errors[1].ToString().ShouldEqual("The databaseInfoName parameter doesn't match the parent's DatabaseInfoName. Set the databaseInfoName parameter to null to use the parent's DatabaseInfoName value.");
    }

    [Fact]
    public async Task TestAddHierarchicalTenantAsyncNoParentHasOwnDbCheck()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tenantIds = await context.BulkLoadHierarchicalTenantShardingAsync();
        context.ChangeTracker.Clear();

        var tenantChange = new StubTenantChangeServiceFactory();
        var service = new AuthTenantAdminService(context,
            _authOptionsHierarchicalSharding, "en".SetupAuthPLoggingLocalizer(),
            tenantChange, null);

        //ATTEMPT
        var status = await service.AddHierarchicalTenantAsync("New Company", 0, null,
            true, "Default Database");

        //VERIFY
        status.IsValid.ShouldBeFalse();
        status.GetAllErrors().ShouldEqual(
            "The hasOwnDb parameter is true, but the sharding database name 'Default Database' already has tenant(s) using that database.");
    }
}