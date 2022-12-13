// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using Net.DistributedFileStoreCache;
using System.Threading.Tasks;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.SupportCode.DownStatusCode;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestExamplesCommonCode;

public class TestSetRemoveStatusService
{
    private readonly ITestOutputHelper _output;
    private readonly IDistributedFileStoreCacheClass _fsCache;

    public TestSetRemoveStatusService(ITestOutputHelper output)
    {
        _output = output;
        _fsCache = new StubFileStoreCacheClass(); //this clears the cache fro each test
    }

    private static AuthPermissionsDbContext SaveTenantToDatabase(Tenant testTenant)
    {
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        options.StopNextDispose();
        var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();
        context.Add(testTenant);

        context.SaveChanges();
        return context;
    }

    [Fact]
    public void TestSetAppDown()
    {
        //SETUP
        var removeService = new SetRemoveStatus(_fsCache, null);

        var dto = new ManuelAppDownDto
        {
            UserId = "123456",
            Message = "hello"
        };

        //ATTEMPT
        removeService.SetAppDown(dto);

        //VERIFY
        var alldowns = removeService.GetAllDownKeyValues();
        alldowns.Count.ShouldEqual(1);
        var downData = removeService.GetAppDownMessage();
        downData.UserId.ShouldEqual("123456");
        downData.Message.ShouldEqual("hello");
    }

    [Fact]
    public void TestRemoveAnyDown()
    {
        //SETUP
        var removeService = new SetRemoveStatus(_fsCache, null);

        var dto = new ManuelAppDownDto
        {
            UserId = "123456",
            Message = "hello"
        };
        removeService.SetAppDown(dto);

        var alldowns = removeService.GetAllDownKeyValues();
        alldowns.Count.ShouldEqual(1);

        //ATTEMPT
        removeService.RemoveAnyDown(alldowns.Single().Key);

        //VERIFY
        removeService.GetAllDownKeyValues().Count.ShouldEqual(0);
    }

    [Theory]
    [InlineData(TenantDownVersions.Update)]
    [InlineData(TenantDownVersions.ManualDown)]
    [InlineData(TenantDownVersions.Deleted)]
    public async Task TestTenantDownSingleTenant(TenantDownVersions downVersion)
    {
        //SETUP
        var testTenant = AuthPSetupHelpers.CreateTestSingleTenantOk("TestTenant");
        SaveTenantToDatabase(testTenant);

        var removeService = new SetRemoveStatus(_fsCache, new StubAuthTenantAdminService(testTenant));

        //ATTEMPT
        await removeService.SetTenantDownWithDelayAsync(downVersion, 1, 0, 1);

        //VERIFY
        var downCacheEntry = removeService.GetAllDownKeyValues().Single();
        downCacheEntry.Key.ShouldStartWith(RedirectUsersViaStatusData.DivertTenantPrefix + downVersion);
        downCacheEntry.Value.ShouldEqual("1.");
    }

    [Theory]
    [InlineData(TenantDownVersions.Update)]
    [InlineData(TenantDownVersions.ManualDown)]
    [InlineData(TenantDownVersions.Deleted)]
    public async Task TestTenantDownSingleTenant_Remove(TenantDownVersions downVersion)
    {
        //SETUP
        var testTenant = AuthPSetupHelpers.CreateTestSingleTenantOk("TestTenant");
        SaveTenantToDatabase(testTenant);

        var removeService = new SetRemoveStatus(_fsCache, new StubAuthTenantAdminService(testTenant));
        var removeFunc = await removeService.SetTenantDownWithDelayAsync(downVersion, 1, 0, 1);
        removeService.GetAllDownKeyValues().Count.ShouldEqual(1);

        //ATTEMPT
        await removeFunc();

        //VERIFY
        removeService.GetAllDownKeyValues().Count.ShouldEqual(0);
    }

    [Fact]
    public async Task TestTenantDown_HierarchicalTenant_NoParent()
    {
        //SETUP
        var testTenant = AuthPSetupHelpers.CreateTestHierarchicalTenantOk("TestTenant", null);
        SaveTenantToDatabase(testTenant);

        var removeService = new SetRemoveStatus(_fsCache, new StubAuthTenantAdminService(testTenant));

        //ATTEMPT
        await removeService.SetTenantDownWithDelayAsync(TenantDownVersions.Update, 1, 0, 1);

        //VERIFY
        var downCacheEntry = removeService.GetAllDownKeyValues().Single();
        downCacheEntry.Key.ShouldStartWith(RedirectUsersViaStatusData.DivertTenantPrefix + TenantDownVersions.Update);
        downCacheEntry.Value.ShouldEqual("1.");
    }

    [Fact]
    public async Task TestTenantDown_HierarchicalTenant_WithParent()
    {
        //SETUP
        var parentTenant = AuthPSetupHelpers.CreateTestHierarchicalTenantOk("Parent", null);
        SaveTenantToDatabase(parentTenant);
        var childTenant = AuthPSetupHelpers.CreateTestHierarchicalTenantOk("Child", parentTenant);
        SaveTenantToDatabase(childTenant);

        var removeService = new SetRemoveStatus(_fsCache, new StubAuthTenantAdminService(parentTenant, childTenant));

        //ATTEMPT
        await removeService.SetTenantDownWithDelayAsync(TenantDownVersions.Update, childTenant.TenantId, parentTenant.TenantId, 1);

        //VERIFY
        var cacheEntries = removeService.GetAllDownKeyValues();
        cacheEntries.Count.ShouldEqual(2);
        cacheEntries.Select(x => x.Value).ShouldEqual(new []{"1.2.", "1."});
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestTenantDown_Sharding(bool hasOwnDb)
    {
        //SETUP
        var testTenant = AuthPSetupHelpers.CreateTestSingleTenantOk("TestTenant", null);
        var context = SaveTenantToDatabase(testTenant);
        testTenant.UpdateShardingState("DatabaseInfoName", hasOwnDb);
        context.SaveChanges();

        var removeService = new SetRemoveStatus(_fsCache, new StubAuthTenantAdminService(testTenant));

        //ATTEMPT
        await removeService.SetTenantDownWithDelayAsync(TenantDownVersions.Update, 1, 0, 1);

        //VERIFY
        var downCacheEntry = removeService.GetAllDownKeyValues().Single();
        downCacheEntry.Key.ShouldStartWith(RedirectUsersViaStatusData.DivertTenantPrefix + TenantDownVersions.Update);

        downCacheEntry.Value.ShouldEqual(hasOwnDb ? "DatabaseInfoName|NoQueryFilter" :  "DatabaseInfoName|1.");
    }
}