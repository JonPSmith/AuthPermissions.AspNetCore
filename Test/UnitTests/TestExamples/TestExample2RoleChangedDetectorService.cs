// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Example2.WebApiWithToken.IndividualAccounts.ClaimsChangeCode;
using System.Threading.Tasks;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Test.StubClasses;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;
using Test.TestHelpers;
using Xunit.Abstractions;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode.DataLayer;
using Microsoft.EntityFrameworkCore;

namespace Test.UnitTests.TestExamples;

public class TestExample2RoleChangedDetectorService
{
    private readonly ITestOutputHelper _output;

    public TestExample2RoleChangedDetectorService(ITestOutputHelper output)
    {
        _output = output;
    }


    [Fact]
    public async Task TestRoleChangedDetectorService_CheckUserAndRoles()
    {
        //SETUP
        var authOptions = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(TestEnum) } };
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var stubFsCache = new StubFileStoreCacheClass();
        var context = new AuthPermissionsDbContext(options, new List<IDatabaseStateChangeEvent> { new RoleChangedDetectorService(stubFsCache, authOptions)});
        context.Database.EnsureCreated();

        await context.SetupRolesInDbAsync();
        context.AddMultipleUsersWithRolesInDb();

        context.ChangeTracker.Clear();
        stubFsCache.ClearAll();

        var authAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(), 
            authOptions, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var users = authAdmin.QueryAuthUsers().Include(x => x.UserRoles).ToList();


        //VERIFY
        foreach (var user in users)
        {
            _output.WriteLine($"UserId = {user.UserId}, Roles = {string.Join(", ", user.UserRoles.Select(x => x.RoleName))}");
        }
    }

    [Fact]
    public async Task TestRoleChangedDetectorService_AddUserToRole()
    {
        //SETUP
        var authOptions = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(TestEnum) } };
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var stubFsCache = new StubFileStoreCacheClass();
                var context = new AuthPermissionsDbContext(options, new List<IDatabaseStateChangeEvent> { new RoleChangedDetectorService(stubFsCache, authOptions)});
        context.Database.EnsureCreated();

        await context.SetupRolesInDbAsync();
        context.AddMultipleUsersWithRolesInDb();

        context.ChangeTracker.Clear();
        stubFsCache.ClearAll();

        var authAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(),
            authOptions, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        await authAdmin.UpdateUserAsync("User1", roleNames: new List<string> { "Role1", "Role2" });

        //VERIFY
        foreach (var user in authAdmin.QueryAuthUsers().Include(x => x.UserRoles).ToList())
        {
            _output.WriteLine($"UserId = {user.UserId}, Roles = {string.Join(", ", user.UserRoles.Select(x => x.RoleName))}");
        }
        var allChanges = stubFsCache.GetAllKeyValues();
        allChanges.Count.ShouldEqual(1);
        allChanges.First().Key.ShouldEqual("ReplacementPermissionsUser1");
        allChanges.First().Value.Select(x => (int)x).ShouldEqual(new []{1,2});
    }

    [Fact]
    public async Task TestRoleChangedDetectorService_RemoveUserToRole()
    {
        //SETUP
        var authOptions = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(TestEnum) } };
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var stubFsCache = new StubFileStoreCacheClass();
                var context = new AuthPermissionsDbContext(options, new List<IDatabaseStateChangeEvent> { new RoleChangedDetectorService(stubFsCache, authOptions)});
        context.Database.EnsureCreated();

        await context.SetupRolesInDbAsync();
        context.AddMultipleUsersWithRolesInDb();

        context.ChangeTracker.Clear();
        stubFsCache.ClearAll();

        var authAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(),
            authOptions, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        await authAdmin.UpdateUserAsync("User3", roleNames: new List<string> { "Role1" });

        //VERIFY
        foreach (var user in authAdmin.QueryAuthUsers().Include(x => x.UserRoles).ToList())
        {
            _output.WriteLine($"UserId = {user.UserId}, Roles = {string.Join(", ", user.UserRoles.Select(x => x.RoleName))}");
        }
        var allChanges = stubFsCache.GetAllKeyValues();
        allChanges.Count.ShouldEqual(1);
        allChanges.First().Key.ShouldEqual("ReplacementPermissionsUser3");
        allChanges.First().Value.Select(x => (int)x).ShouldEqual(new[] { 1 });
    }

    [Fact]
    public async Task TestRoleChangedDetectorService_NoRoles()
    {
        //SETUP
        var authOptions = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(TestEnum) } };
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var stubFsCache = new StubFileStoreCacheClass();
        var context = new AuthPermissionsDbContext(options, new List<IDatabaseStateChangeEvent> { new RoleChangedDetectorService(stubFsCache, authOptions) });
        context.Database.EnsureCreated();

        await context.SetupRolesInDbAsync();
        context.AddMultipleUsersWithRolesInDb();

        context.ChangeTracker.Clear();
        stubFsCache.ClearAll();

        var authAdmin = new AuthUsersAdminService(context, new StubSyncAuthenticationUsersFactory(),
            authOptions, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        await authAdmin.UpdateUserAsync("User2", roleNames: new List<string> { });

        //VERIFY
        foreach (var user in authAdmin.QueryAuthUsers().Include(x => x.UserRoles).ToList())
        {
            _output.WriteLine($"UserId = {user.UserId}, Roles = {string.Join(", ", user.UserRoles.Select(x => x.RoleName))}");
        }
        var allChanges = stubFsCache.GetAllKeyValues();
        allChanges.Count.ShouldEqual(1);
        allChanges.First().Key.ShouldEqual("ReplacementPermissionsUser2");
        allChanges.First().Value.ShouldEqual("");
    }

    [Fact]
    public async Task TestRoleChangedDetectorService_ChangeRoleToPermissions()
    {
        //SETUP
        var authOptions = new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(TestEnum) } };
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        var stubFsCache = new StubFileStoreCacheClass();
                var context = new AuthPermissionsDbContext(options, new List<IDatabaseStateChangeEvent> { new RoleChangedDetectorService(stubFsCache, authOptions)});
        context.Database.EnsureCreated();

        await context.SetupRolesInDbAsync();
        context.AddMultipleUsersWithRolesInDb();

        context.ChangeTracker.Clear();
        stubFsCache.ClearAll();

        var rolesAdmin = new AuthRolesAdminService(context, authOptions, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        await rolesAdmin.UpdateRoleToPermissionsAsync("Role2", new List<string> { "One", "Three" }, null);

        //VERIFY
        foreach (var role in context.RoleToPermissions.ToList())
        {
            _output.WriteLine($"RoleName = {role.RoleName}, Permissions = {string.Join(", ", role.PackedPermissionsInRole.Select(x => (int)x))}");
        }
        var allChanges = stubFsCache.GetAllKeyValues();
        allChanges.Count.ShouldEqual(2);
        allChanges.Keys.ShouldEqual(new []{ "ReplacementPermissionsUser2", "ReplacementPermissionsUser3" });
        allChanges["ReplacementPermissionsUser2"].Select(x => (int)x).ShouldEqual(new[] { 1, 3 });
        allChanges["ReplacementPermissionsUser3"].Select(x => (int)x).ShouldEqual(new[] { 1, 3 });
    }
}