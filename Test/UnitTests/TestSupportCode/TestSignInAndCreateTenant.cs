// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.SupportCode;
using AuthPermissions.SupportCode.AddUsersServices;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using Example7.MvcWebApp.ShardingOnly.PermissionsCode;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestSupportCode;

public class TestSignInAndCreateTenant
{
    private readonly ITestOutputHelper _output;
    private static List<LogOutput> _logs;

    public TestSignInAndCreateTenant(ITestOutputHelper output)
    {
        _output = output;
    }

    private static (SignInAndCreateTenant service, AuthUsersAdminService userAdmin)
        CreateISignInAndCreateTenant(AuthPermissionsDbContext context,
            TenantTypes tenantType = TenantTypes.NotUsingTenants,
            ISignUpGetShardingEntry overrideNormal = null,
            bool loginReturnsError = false)
    {
        var authOptions = new AuthPermissionsOptions
        {
            TenantType = tenantType
        };
        var userAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(), 
            authOptions, "en".SetupAuthPLoggingLocalizer());
        var tenantAdmin = new AuthTenantAdminService(context, authOptions,
            "en".SetupAuthPLoggingLocalizer(), new StubTenantChangeServiceFactory(), null);
        _logs = new List<LogOutput>();
        ILogger<SignInAndCreateTenant> logger = new LoggerFactory(
                new[] { new MyLoggerProviderActionOut(log => _logs.Add(log)) })
            .CreateLogger<SignInAndCreateTenant>();
        var service = new SignInAndCreateTenant(authOptions, tenantAdmin,
            new StubAddNewUserManager(userAdmin, tenantAdmin, loginReturnsError), 
            "en".SetupAuthPLoggingLocalizer(), logger,
            overrideNormal ?? new StubISignUpGetShardingEntry("en".SetupAuthPLoggingLocalizer(), false));

        return (service, userAdmin);
    }

    [Theory]
    [InlineData("Free", null, "Invoice Creator,Invoice Reader")]
    [InlineData("Pro", "Tenant Admin", "Invoice Creator,Invoice Reader,Tenant Admin")]
    [InlineData("Enterprise", "Enterprise,Tenant Admin", "Invoice Creator,Invoice Reader,Tenant Admin")]
    public async Task TestAddUserAndNewTenantAsync_Example3Version(string version, string tenantRoles, string adminRoles)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = CreateISignInAndCreateTenant(context, TenantTypes.SingleLevel);
        var authSettings = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(Example3Permissions) } };
        var rolesSetup = new BulkLoadRolesService(context, authSettings);
        await rolesSetup.AddRolesToDatabaseAsync(Example3AppAuthSetupData.RolesDefinition);

        context.ChangeTracker.Clear();

        //ATTEMPT
        var userData = new AddNewUserDto{Email = "me!@g1.com"};
        var tenantData = new AddNewTenantDto { TenantName = "New Tenant", Version = version };
        var status = await tuple.service.SignUpNewTenantWithVersionAsync(userData, tenantData, Example3CreateTenantVersions.TenantSetupData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        status.Result.ShouldNotBeNull();
        var tenant = context.Tenants.Include(x => x.TenantRoles).Single();
        tenant.TenantFullName.ShouldEqual(tenantData.TenantName);
        tenant.TenantRoles.Select(x => x.RoleName).ToArray()
            .ShouldEqual(tenantRoles?.Split(',') ?? Array.Empty<string>());
        var user = context.AuthUsers.Include(x => x.UserRoles).Single();
        user.UserRoles.Select(x => x.RoleName).ToArray().ShouldEqual(adminRoles.Split(','));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestAddUserAndNewTenantAsync_NoVersionSetup(bool hasOwnDb)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = CreateISignInAndCreateTenant(context, TenantTypes.SingleLevel | TenantTypes.AddSharding);
        await context.SetupRolesInDbAsync();

        context.ChangeTracker.Clear();

        //ATTEMPT
        var userData = new AddNewUserDto
        {
            Email = "me!@g1.com",
            Roles = new List<string> { "Role1", "Role3" }
        };
        var tenantData = new AddNewTenantDto { TenantName = "New Tenant", HasOwnDb = hasOwnDb};
        var status = await tuple.service.SignUpNewTenantAsync(userData, tenantData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        var tenant = context.Tenants.Include(x => x.TenantRoles).Single();
        tenant.TenantFullName.ShouldEqual("New Tenant");
        tenant.TenantRoles.Count.ShouldEqual(0);
        tenant.HasOwnDb.ShouldEqual(hasOwnDb);
        var user = context.AuthUsers.Include(x => x.UserRoles).Single();
        user.UserRoles.Select(x => x.RoleName).ToArray().ShouldEqual(new []{ "Role1", "Role3" });
    }


    [Fact]
    public async Task TestAddUserAndNewTenantAsync_Shared()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = CreateISignInAndCreateTenant(context, TenantTypes.SingleLevel, null);
        var authSettings = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(Example3Permissions) } };
        var rolesSetup = new BulkLoadRolesService(context, authSettings);
        await rolesSetup.AddRolesToDatabaseAsync(Example3AppAuthSetupData.RolesDefinition);

        var userData = new AddNewUserDto { Email = "me!@g1.com" };
        var tenantData = new AddNewTenantDto
        {
            TenantName = "New Tenant",
            Region = "South",
            Version = "Free"
        };
        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await tuple.service.SignUpNewTenantWithVersionAsync(userData, tenantData, Example3CreateTenantVersions.TenantSetupData);

        //VERIFY
        context.ChangeTracker.Clear();
        var log = _logs.LastOrDefault();
        if (log != null)
            _output.WriteLine(log.DecodeMessage());
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        var tenant = context.Tenants.Single();
        tenant.TenantFullName.ShouldEqual(tenantData.TenantName);
        tenant.DatabaseInfoName.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TestAddUserAndNewTenantAsync_ShardingHybrid(bool hasOwnDb)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = CreateISignInAndCreateTenant(context, TenantTypes.SingleLevel | TenantTypes.AddSharding);
        var authSettings = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(Example3Permissions) } };
        var rolesSetup = new BulkLoadRolesService(context, authSettings);
        await rolesSetup.AddRolesToDatabaseAsync(Example3AppAuthSetupData.RolesDefinition);

        var userData = new AddNewUserDto { Email = "me!@g1.com" };
        var tenantData = new AddNewTenantDto
        {
            TenantName = "New Tenant",
            Region = "South",
            HasOwnDb = hasOwnDb,
        };
        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await tuple.service.SignUpNewTenantAsync(userData, tenantData);

        //VERIFY
        context.ChangeTracker.Clear();
        var log = _logs.LastOrDefault();
        if (log != null)
            _output.WriteLine(log.DecodeMessage());
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        var tenant = context.Tenants.Single();
        _output.WriteLine(tenant.DatabaseInfoName);
        tenant.TenantFullName.ShouldEqual(tenantData.TenantName);
        if (hasOwnDb)
            tenant.DatabaseInfoName.ShouldStartWith("SignOn-");
        else
            tenant.DatabaseInfoName.ShouldEqual("Default Database");
    }

    [Fact]
    public async Task TestAddUserAndNewTenantAsync_ShardingOnly()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var getSetShardings = new StubGetSetShardingEntries(this);

        var tuple = CreateISignInAndCreateTenant(context, TenantTypes.SingleLevel | TenantTypes.AddSharding
            , new DemoShardOnlyGetDatabaseForNewTenant(getSetShardings, "en".SetupAuthPLoggingLocalizer()));
        var authSettings = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(Example3Permissions) } };
        var rolesSetup = new BulkLoadRolesService(context, authSettings);
        await rolesSetup.AddRolesToDatabaseAsync(Example3AppAuthSetupData.RolesDefinition);

        var userData = new AddNewUserDto { Email = "me!@g1.com" };
        var tenantData = new AddNewTenantDto
        {
            TenantName = "New Tenant",
            Region = "South",
            Version = "Free",
            HasOwnDb = true,
        };
        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await tuple.service.SignUpNewTenantWithVersionAsync(userData, tenantData, Example7CreateTenantVersions.TenantSetupData);

        //VERIFY
        context.ChangeTracker.Clear();
        var log = _logs.LastOrDefault();
        if (log != null)
            _output.WriteLine(log.DecodeMessage());
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        var tenant = context.Tenants.Single();
        _output.WriteLine(tenant.DatabaseInfoName);
        tenant.TenantFullName.ShouldEqual(tenantData.TenantName);
        tenant.DatabaseInfoName.ShouldStartWith("SignOn-");
    }

    [Fact]
    public async Task TestAddUserAndNewTenantAsync_ExistingTenant()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var tuple = CreateISignInAndCreateTenant(context, TenantTypes.SingleLevel);
        var authSettings = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(Example3Permissions) } };
        var rolesSetup = new BulkLoadRolesService(context, authSettings);
        await rolesSetup.AddRolesToDatabaseAsync(Example3AppAuthSetupData.RolesDefinition);
        context.Add(Tenant.CreateSingleTenant("Existing Tenant", new StubDefaultLocalizer()).Result);
        context.SaveChanges();

        context.ChangeTracker.Clear();

        //ATTEMPT
        var userData = new AddNewUserDto { Email = "me!@g1.com" };
        var tenantData = new AddNewTenantDto { TenantName = "Existing Tenant" };
        var status = await tuple.service.SignUpNewTenantWithVersionAsync(userData, tenantData, Example3CreateTenantVersions.TenantSetupData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeFalse();
        status.GetAllErrors().ShouldEqual("The tenant name 'Existing Tenant' is already taken.");
    }

    [Fact]
    public async Task TestAddUserAndNewTenantAsync_Sharding_UndoTenant()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var getDbCauseError = new StubISignUpGetShardingEntry("en".SetupAuthPLoggingLocalizer(), true);
        var tuple = CreateISignInAndCreateTenant(context, TenantTypes.SingleLevel | TenantTypes.AddSharding, getDbCauseError);
        var authSettings = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(Example3Permissions) } };
        var rolesSetup = new BulkLoadRolesService(context, authSettings);
        await rolesSetup.AddRolesToDatabaseAsync(Example3AppAuthSetupData.RolesDefinition);

        var userData = new AddNewUserDto { Email = "Me!@g1.com"};
        var tenantData = new AddNewTenantDto
        {
            TenantName = "New Tenant",
            Version = "Free",
            HasOwnDb = true,
        };
        Example3CreateTenantVersions.TenantSetupData.HasOwnDbForEachVersion = new Dictionary<string, bool?>()
        {
            { "Free", true },
        };

        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await tuple.service.SignUpNewTenantWithVersionAsync(userData, tenantData, Example3CreateTenantVersions.TenantSetupData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeFalse();
        context.Tenants.Count().ShouldEqual(0);
    }

    [Fact]
    public async Task TestAddUserAndNewTenantAsync_Sharding_UndoOnBadUser()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var getDbCauseError = new StubISignUpGetShardingEntry("en".SetupAuthPLoggingLocalizer(), true);
        var tuple = CreateISignInAndCreateTenant(context, TenantTypes.SingleLevel | TenantTypes.AddSharding, 
            getDbCauseError, true);
        var authSettings = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(Example3Permissions) } };
        var rolesSetup = new BulkLoadRolesService(context, authSettings);
        await rolesSetup.AddRolesToDatabaseAsync(Example3AppAuthSetupData.RolesDefinition);

        var userData = new AddNewUserDto { Email = "me@g.com" };
        var tenantData = new AddNewTenantDto
        {
            TenantName = "New Tenant",
            Version = "Free",
            HasOwnDb = true,
        };
        Example3CreateTenantVersions.TenantSetupData.HasOwnDbForEachVersion = new Dictionary<string, bool?>()
        {
            { "Free", true },
        };

        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await tuple.service.SignUpNewTenantWithVersionAsync(userData, tenantData, Example3CreateTenantVersions.TenantSetupData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeFalse();
        context.Tenants.Count().ShouldEqual(0);
    }

    //----------------------------------------------------------
    //Test the two demo version of the ISignUpGetShardingEntry 

    [Fact]
    public async Task DemoGetDatabaseForNewTenant()
    {
        //SETUP
        var getSetSharding = new StubGetSetShardingEntries(this);
        var service = new DemoGetDatabaseForNewTenant(getSetSharding, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var status = await service.FindOrCreateShardingEntryAsync(true, "timestamp", "SouthDb");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        status.Result.ShouldEqual("PostgreSql1");
    }

    [Fact]
    public async Task DemoShardOnlyGetDatabaseForNewTenant()
    {
        //SETUP
        var getSetSharding = new StubGetSetShardingEntries(this);
        var service = new DemoShardOnlyGetDatabaseForNewTenant(getSetSharding, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var status = await service.FindOrCreateShardingEntryAsync(true, "timestamp", "SouthDb");

        //VERIFY
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        getSetSharding.CalledMethodName.ShouldEqual("AddNewShardingEntry");
        getSetSharding.SharingEntryAddUpDel.Name.ShouldEqual("SignOn-timestamp");
        getSetSharding.SharingEntryAddUpDel.ConnectionName.ShouldEqual("SouthDb");
        getSetSharding.SharingEntryAddUpDel.DatabaseName.ShouldEqual("Db-timestamp");
        getSetSharding.SharingEntryAddUpDel.DatabaseType.ShouldEqual("SqlServer");
    }
}