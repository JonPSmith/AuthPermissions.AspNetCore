// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using AuthPermissions.AdminCode.Services;
using AuthPermissions.AspNetCore.ShardingServices;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.SupportCode.AddUsersServices;
using Example3.MvcWebApp.IndividualAccounts.PermissionsCode;
using LocalizeMessagesAndErrors.UnitTestingCode;
using Microsoft.EntityFrameworkCore;
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

    public TestSignInAndCreateTenant(ITestOutputHelper output)
    {
        _output = output;
    }

    private static (SignInAndCreateTenant service, AuthUsersAdminService userAdmin)
        CreateISignInAndCreateTenant(AuthPermissionsDbContext context,
            TenantTypes tenantType = TenantTypes.NotUsingTenants,
            IGetDatabaseForNewTenant overrideNormal = null,
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
        var service = new SignInAndCreateTenant(authOptions, tenantAdmin,
            new StubAddNewUserManager(userAdmin, tenantAdmin, loginReturnsError), context,
            "en".SetupAuthPLoggingLocalizer(),
            overrideNormal ?? new StubIGetDatabaseForNewTenant(context, false));

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

    [Theory]
    [InlineData(true,  null, "OwnDb")]
    [InlineData(false, null, "SharedDb")]
    [InlineData(true, false, "OwnDb")] //The AddNewTenantDto.HasOwnDb is overridden by the version setup data
    [InlineData(false, true, "SharedDb")] //The AddNewTenantDto.HasOwnDb is overridden by the version setup data
    public async Task TestAddUserAndNewTenantAsync_Sharding(bool? setupHasOwnDb, bool? dtoHasOwnDb,
        string databaseInfoName)
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
            Version = "Free",
            HasOwnDb = dtoHasOwnDb,
        };
        Example3CreateTenantVersions.TenantSetupData.HasOwnDbForEachVersion = new Dictionary<string, bool?>()
        {
            { "Free", setupHasOwnDb },
        };

        context.ChangeTracker.Clear();

        //ATTEMPT
        var status = await tuple.service.SignUpNewTenantWithVersionAsync(userData, tenantData, Example3CreateTenantVersions.TenantSetupData);

        //VERIFY
        context.ChangeTracker.Clear();
        status.IsValid.ShouldBeTrue(status.GetAllErrors());
        var tenant = context.Tenants.Single();
        tenant.TenantFullName.ShouldEqual(tenantData.TenantName);
        tenant.DatabaseInfoName.ShouldEqual(databaseInfoName);
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

        var getDbCauseError = new StubIGetDatabaseForNewTenant(context,true);
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
        status.IsValid.ShouldBeFalse(status.GetAllErrors());
        context.Tenants.Count().ShouldEqual(0);
    }

    [Fact]
    public async Task TestAddUserAndNewTenantAsync_Sharding_UndoOnBadUser()
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var getDbCauseError = new StubIGetDatabaseForNewTenant(context,false);
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
        getDbCauseError.RemoveLastDatabaseCalled.ShouldBeTrue();
    }
}