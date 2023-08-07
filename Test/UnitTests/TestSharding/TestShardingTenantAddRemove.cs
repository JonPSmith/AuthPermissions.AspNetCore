// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.SetupCode;
using Test.StubClasses;
using Test.TestHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSharding;

public class TestShardingOnlyTenantAddRemove
{
    private readonly ITestOutputHelper _output;

    private AuthPermissionsOptions _authPOptions;
    private StubGetSetShardingEntries _getSetShardings;
    private StubAuthTenantAdminService _stubTenantAdmin;

    public TestShardingOnlyTenantAddRemove(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// This returns an instance of the <see cref="ShardingOnlyTenantAddRemove"/> with the TenantType set.
    /// It also creates a extra <see cref="Tenant"/> to check duplication errors and also for the Delete
    /// </summary>
    /// <param name="tenantType"></param>
    /// <param name="childTenant"></param>
    /// <returns></returns>
    private ShardingOnlyTenantAddRemove SetupService(TenantTypes tenantType = TenantTypes.SingleLevel,
        bool childTenant = false)
    {
        _authPOptions = new AuthPermissionsOptions
        {
            TenantType = tenantType | TenantTypes.AddSharding
        };

        var demoTenant = tenantType == TenantTypes.SingleLevel
            ? "TenantSingle".CreateSingleShardingTenant("Other Database", true)
            : "TenantHierarchical".CreateHierarchicalShardingTenant("Other Database", true);
        if (tenantType == TenantTypes.HierarchicalTenant && childTenant)
        {
            demoTenant =
                "TenantHierarchicalChild".CreateHierarchicalShardingTenant("Other Database", true, demoTenant);
        }

        _getSetShardings = new StubGetSetShardingEntries(this);
        _stubTenantAdmin = new StubAuthTenantAdminService(demoTenant);
        return new ShardingOnlyTenantAddRemove(_stubTenantAdmin, _getSetShardings,
            _authPOptions, "en".SetupAuthPLoggingLocalizer());
    }

    //---------------------------------------------------
    // Create Single

    [Fact]
    public async Task Create_Single_Good()
    {
        //SETUP
        var dto = new ShardingOnlyTenantAddDto
        {
            TenantName = "Test",

            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };
        var service = SetupService();

        //ATTEMPT
        var status = await service.CreateTenantAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(_getSetShardings.SharingEntryAddUpDel.ToString());
        _getSetShardings.SharingEntryAddUpDel.Name.ShouldStartWith("Test-");
        _getSetShardings.SharingEntryAddUpDel.ConnectionName.ShouldEqual("DefaultConnection");
        _getSetShardings.SharingEntryAddUpDel.DatabaseType.ShouldEqual("SqlServer");
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddSingleTenantAsync");
    }

    [Fact]
    public async Task Create_Single_DuplicateTenant()
    {
        //SETUP
        var dto = new ShardingOnlyTenantAddDto
        {
            TenantName = "TenantSingle",
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };
        var service = SetupService();

        //ATTEMPT
        var status = await service.CreateTenantAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeTrue();
        status.GetAllErrors().ShouldEqual("The tenant name 'TenantSingle' is already used");
    }

    //---------------------------------------------------
    // Create Hierarchical

    [Fact]
    public async Task Create_Hierarchical_TopLevel_Good()
    {
        //SETUP
        var dto = new ShardingOnlyTenantAddDto
        {
            TenantName = "Test",
 
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };
        var service = SetupService(TenantTypes.HierarchicalTenant);

        //ATTEMPT
        var status = await service.CreateTenantAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(_getSetShardings.SharingEntryAddUpDel.ToString());
        _getSetShardings.SharingEntryAddUpDel.Name.ShouldStartWith("Test-");
        _getSetShardings.SharingEntryAddUpDel.ConnectionName.ShouldEqual("DefaultConnection");
        _getSetShardings.SharingEntryAddUpDel.DatabaseType.ShouldEqual("SqlServer");
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddHierarchicalTenantAsync");
    }


    //Its very hard to test this 
    //[Fact]
    //public async Task Create_Hierarchical_Child_Good()
    //{
    //    //SETUP
    //    var dto = new ShardingOnlyTenantAddDto
    //    {
    //        TenantName = "Test",
    //        HasOwnDb = true,
    //        ParentTenantId = 1
    //    };
    //    var service = SetupService(true, TenantTypes.HierarchicalTenant, true);

    //    //ATTEMPT
    //    var status = await service.CreateShardingTenantAndConnectionAsync(dto);

    //    //VERIFY
    //    status.HasErrors.ShouldBeFalse(status.GetAllErrors());
    //    _getSetShardings.SharingEntryAddUpDel.ShouldBeNull();
    //    _stubTenantAdmin.CalledMethodName.ShouldEqual("AddHierarchicalTenantAsync");
    //}

    //---------------------------------------------------
    // Delete Single

    [Fact]
    public async Task Delete_Single_Good()
    {
        //SETUP
        var service = SetupService();

        ////ATTEMPT
        var status = await service.DeleteTenantAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _getSetShardings.CalledMethodName.ShouldEqual("RemoveShardingEntry");
        _getSetShardings.SharingEntryAddUpDel.Name.ShouldEqual("Other Database");
    }

    //---------------------------------------------------
    // Delete Hierarchical

    [Fact]
    public async Task Delete_Hierarchical_Good()
    {
        //SETUP
        var service = SetupService(TenantTypes.HierarchicalTenant);

        ////ATTEMPT
        var status = await service.DeleteTenantAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _getSetShardings.CalledMethodName.ShouldEqual("RemoveShardingEntry");
        _getSetShardings.SharingEntryAddUpDel.Name.ShouldEqual("Other Database");
    }
}