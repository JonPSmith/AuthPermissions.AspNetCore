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

public class TestShardingTenantAddRemove
{
    private readonly ITestOutputHelper _output;

    private AuthPermissionsOptions _authPOptions;
    private StubGetSetShardingEntries _getSetShardings;
    private StubAuthTenantAdminService _stubTenantAdmin;

    public TestShardingTenantAddRemove(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// This returns an instance of the <see cref="ShardingTenantAddRemove"/> with the TenantType set.
    /// It also creates a extra <see cref="Tenant"/> to check duplication errors and also for the Delete
    /// </summary>
    /// <param name="hasOwnDb"></param>
    /// <param name="tenantType"></param>
    /// <param name="childTenant"></param>
    /// <returns></returns>
    private ShardingTenantAddRemove SetupService(bool hasOwnDb, TenantTypes tenantType = TenantTypes.SingleLevel,
         bool childTenant = false)
    {
        _authPOptions = new AuthPermissionsOptions
        {
            TenantType = tenantType | TenantTypes.AddSharding
        };

        var demoTenant = tenantType == TenantTypes.SingleLevel
            ? "TenantSingle".CreateSingleShardingTenant("Other Database", hasOwnDb)
            : "TenantHierarchical".CreateHierarchicalShardingTenant("Other Database", hasOwnDb);
        if (tenantType == TenantTypes.HierarchicalTenant && childTenant)
        {
            demoTenant =
                "TenantHierarchicalChild".CreateHierarchicalShardingTenant("Other Database", hasOwnDb, demoTenant);
        }

        _getSetShardings = new StubGetSetShardingEntries(this);
        _stubTenantAdmin = new StubAuthTenantAdminService(demoTenant);
        return new ShardingTenantAddRemove(_stubTenantAdmin, _getSetShardings,
            _authPOptions, "en".SetupAuthPLoggingLocalizer());
    }

    //---------------------------------------------------
    // Create Single

    [Fact]
    public async Task Create_Single_HasOwnDbTrue_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = true,
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };
        var service = SetupService(true);

        //ATTEMPT
        var status = await service.CreateTenantAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(_getSetShardings.SharingEntryAddUpDel.ToString());
        _getSetShardings.SharingEntryAddUpDel.Name.ShouldEndWith("-Test");
        _getSetShardings.SharingEntryAddUpDel.ConnectionName.ShouldEqual("DefaultConnection");
        _getSetShardings.SharingEntryAddUpDel.DatabaseType.ShouldEqual("SqlServer");
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddSingleTenantAsync");
    }

    [Fact]
    public async Task Create_Single_HasOwnDbTrue_Good_OverriddenByDatabaseInfoName()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = true,
            ShardingEntityName = "Default Database"
        };
        var service = SetupService(true);

        //ATTEMPT
        var status = await service.CreateTenantAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddSingleTenantAsync");
    }

    [Fact]
    public async Task Create_Single_HasOwnDbTrue_DuplicateTenant()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "TenantSingle",
            HasOwnDb = true,
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };
        var service = SetupService(true);

        //ATTEMPT
        var status = await service.CreateTenantAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeTrue();
        status.GetAllErrors().ShouldEqual("The tenant name 'TenantSingle' is already used");
    }

    [Fact]
    public async Task Create_Single_HasOwnDbFalse_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = false,
            ShardingEntityName = "Other Database"
        };
        var service = SetupService(false);

        //ATTEMPT
        var status = await service.CreateTenantAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(status.Message);
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddSingleTenantAsync");
    }

    //---------------------------------------------------
    // Create Hierarchical

    [Fact]
    public async Task Create_Hierarchical_TopLevel_HasOwnDbTrue_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = true,
            ConnectionStringName = "DefaultConnection",
            DbProviderShortName = "SqlServer",
        };
        var service = SetupService(false, TenantTypes.HierarchicalTenant);

        //ATTEMPT
        var status = await service.CreateTenantAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(_getSetShardings.SharingEntryAddUpDel.ToString());
        _getSetShardings.SharingEntryAddUpDel.Name.ShouldEndWith("-Test");
        _getSetShardings.SharingEntryAddUpDel.ConnectionName.ShouldEqual("DefaultConnection");
        _getSetShardings.SharingEntryAddUpDel.DatabaseType.ShouldEqual("SqlServer");
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddHierarchicalTenantAsync");
    }

    [Fact]
    public async Task Create_Hierarchical_TopLevel_HasOwnDbFalse_Good()
    {
        //SETUP
        var dto = new ShardingTenantAddDto
        {
            TenantName = "Test",
            HasOwnDb = false,
            ShardingEntityName = "Other Database"
        };
        var service = SetupService(false, TenantTypes.HierarchicalTenant);

        //ATTEMPT
        var status = await service.CreateTenantAsync(dto);

        //VERIFY
        status.HasErrors.ShouldBeFalse(status.GetAllErrors());
        _output.WriteLine(status.Message);
        _stubTenantAdmin.CalledMethodName.ShouldEqual("AddHierarchicalTenantAsync");
    }

    //Its very hard to test this 
    //[Fact]
    //public async Task Create_Hierarchical_Child_Good()
    //{
    //    //SETUP
    //    var dto = new ShardingTenantAddDto
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
    // Create Single

    [Fact]
    public async Task Delete_Single_HasOwnDbTrue_Good()
    {
        //SETUP
        var service = SetupService(true);

        ////ATTEMPT
        var status = await service.DeleteTenantAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _getSetShardings.CalledMethodName.ShouldEqual("RemoveShardingEntry");
        _getSetShardings.SharingEntryAddUpDel.Name.ShouldEqual("Other Database");
    }

    [Fact]
    public async Task Delete_Single_HasOwnDbFalse_Good()
    {
        //SETUP
        var service = SetupService(false);

        ////ATTEMPT
        var status = await service.DeleteTenantAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _getSetShardings.CalledMethodName.ShouldBeNull();
    }

    //---------------------------------------------------
    // Delete Hierarchical

    [Fact]
    public async Task Delete_Hierarchical_HasOwnDbTrue_Good()
    {
        //SETUP
        var service = SetupService(true, TenantTypes.HierarchicalTenant);

        ////ATTEMPT
        var status = await service.DeleteTenantAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _getSetShardings.CalledMethodName.ShouldEqual("RemoveShardingEntry");
        _getSetShardings.SharingEntryAddUpDel.Name.ShouldEqual("Other Database");
    }

    [Fact]
    public async Task Delete_Hierarchical_HasOwnDbFalse_Good()
    {
        //SETUP
        var service = SetupService(false, TenantTypes.HierarchicalTenant);

        ////ATTEMPT
        var status = await service.DeleteTenantAsync(0);

        ////VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        _stubTenantAdmin.CalledMethodName.ShouldEqual("DeleteTenantAsync");
        _getSetShardings.CalledMethodName.ShouldBeNull();
    }
}