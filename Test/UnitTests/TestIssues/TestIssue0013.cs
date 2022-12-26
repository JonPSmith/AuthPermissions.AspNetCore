// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestIssues;

/// <summary>
/// See https://github.com/JonPSmith/AuthPermissions.AspNetCore/issues/13
/// </summary>
public class TestIssue0013
{
    private readonly AuthPermissionsOptions _authOptionsWithTestEnum =
        new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(TestEnum) } };
    private readonly ITestOutputHelper _output;

    public TestIssue0013(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [ClassData(typeof(AllPossibleRoleTypeChanges))]
    public async Task TestUpdateRoleToPermissionsAsync_(RoleTypes originalType, RoleTypes updatedType, bool hasErrors)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        await SetupRoleUserAndPossibleTenant(originalType, context);
        context.ChangeTracker.Clear();

        var service = new AuthRolesAdminService(context, _authOptionsWithTestEnum, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var status = await service.UpdateRoleToPermissionsAsync("Role1", new[] { "One" }, null, updatedType);

        //VERIFY
        if (status.HasErrors)
            _output.WriteLine(status.GetAllErrors());
        status.HasErrors.ShouldEqual(hasErrors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteRoleAsyncWithUsers(bool removeFromUser)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        await SetupRoleUserAndPossibleTenant(RoleTypes.TenantAdminAdd, context);
        context.ChangeTracker.Clear();

        var service = new AuthRolesAdminService(context, _authOptionsWithTestEnum, "en".SetupAuthPLoggingLocalizer());

        //ATTEMPT
        var status = await service.DeleteRoleAsync("Role1", removeFromUser);

        //VERIFY
        status.IsValid.ShouldEqual(removeFromUser);
        if (status.IsValid)
        {
            context.ChangeTracker.Clear();
            service.QueryUsersUsingThisRole("Role1").Count().ShouldEqual(0);
            status.Message.ShouldEqual("Successfully deleted the role Role1 and removed that role from 1 users and removed that role from 1 tenants.");
        }
    }


    //-------------------------------------------------------------
    //setup code

    private static async Task SetupRoleUserAndPossibleTenant(RoleTypes originalType, AuthPermissionsDbContext context)
    {
        var role = new RoleToPermissions("Role1", null, $"{(char)1}{(char)3}", originalType);
        context.Add(role);
        await context.SaveChangesAsync();
        Tenant tenant = null;
        if (originalType != RoleTypes.HiddenFromTenant)
        {
            var tenantRoles = originalType == RoleTypes.TenantAutoAdd || originalType == RoleTypes.TenantAdminAdd
                ? new List<RoleToPermissions> { role }
                : new List<RoleToPermissions>();
            tenant = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1", tenantRoles);
        }

        if (originalType == RoleTypes.Normal
            || originalType == RoleTypes.TenantAdminAdd
            || originalType == RoleTypes.HiddenFromTenant)
        {
            var authUser = AuthPSetupHelpers.CreateTestAuthUserOk("User1", "User1@g.com", null,
                new List<RoleToPermissions> { role }, tenant);
            context.Add(authUser);
        }

        context.SaveChanges();
    }

    private class AllPossibleRoleTypeChanges : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            var lines = AllOptions.Split(Environment.NewLine);
            foreach (var line in lines)
            {
                var parts = line.Split(' ').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                var originalType = Enum.Parse<RoleTypes>(parts[0]);
                parts.RemoveAt(0);
                var newType = Enum.Parse<RoleTypes>(parts[0]);
                parts.RemoveAt(0);
                var hasError = parts.Select(x => x.Trim()).Contains("ERROR");
                yield return new object[] { originalType, newType, hasError };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// This is taken from https://github.com/JonPSmith/AuthPermissions.AspNetCore/issues/13#issuecomment-1042713484
        /// </summary>
        private const string AllOptions = @"Normal 	TenantAutoAdd 	ERROR 	impossible
Normal 	TenantAdminAdd 	ERROR 	impossible
Normal 	HiddenFromTenant 	ERROR (if user has tenant) 	impossible
TenantAutoAdd 	Normal 	impossible 	ERROR
TenantAutoAdd 	TenantAdminAdd 	impossible 	OK
TenantAutoAdd 	HiddenFromTenant 	impossible 	ERROR
TenantAdminAdd 	Normal 	impossible 	OK
TenantAdminAdd 	TenantAutoAdd 	ERROR 	OK
TenantAdminAdd 	HiddenFromTenant 	ERROR (if user has tenant) 	ERROR
HiddenFromTenant 	Normal 	OK 	impossible
HiddenFromTenant 	TenantAutoAdd 	ERROR 	impossible
HiddenFromTenant 	TenantAdminAdd 	ERROR 	impossible";
    }

}