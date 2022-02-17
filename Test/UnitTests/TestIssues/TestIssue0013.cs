// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode.Services;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using EntityFramework.Exceptions.SqlServer;
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
    private readonly ITestOutputHelper _output;

    public TestIssue0013(ITestOutputHelper output)
    {
        _output = output;
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

    [Theory]
    [ClassData(typeof(AllPossibleRoleTypeChanges))]
    public async Task TestUpdateRoleToPermissionsAsync_(RoleTypes originalType, RoleTypes updatedType, bool hasErrors)
    {
        //SETUP
        var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
        using var context = new AuthPermissionsDbContext(options);
        context.Database.EnsureCreated();

        var role = new RoleToPermissions("Role1", null, $"{(char)1}{(char)3}", originalType);
        context.Add(role);
        await context.SaveChangesAsync();
        Tenant tenant = null;
        if (originalType != RoleTypes.HiddenFromTenant)
        {
            var tenantRoles = originalType == RoleTypes.TenantAutoAdd || originalType == RoleTypes.TenantAdminAdd
            ? new List<RoleToPermissions> { role }
            : new List<RoleToPermissions>( );
            tenant = Tenant.CreateSingleTenant("Tenant1", tenantRoles).Result;
        }

        if (originalType == RoleTypes.Normal 
            || originalType == RoleTypes.TenantAdminAdd 
            || originalType == RoleTypes.HiddenFromTenant)
        {
            var userStatus = AuthUser.CreateAuthUser("User1", "User1@g.com", null, 
                new List<RoleToPermissions> { role }, tenant);
            userStatus.IsValid.ShouldBeTrue(userStatus.GetAllErrors());
            context.Add(userStatus.Result);
        }
        context.SaveChanges();

        context.ChangeTracker.Clear();

        var service = new AuthRolesAdminService(context, new AuthPermissionsOptions { InternalData = { EnumPermissionsType = typeof(TestEnum) } });

        //ATTEMPT
        var status = await service.UpdateRoleToPermissionsAsync("Role1", new[] { "One" }, null, updatedType);

        //VERIFY
        if (status.HasErrors)
            _output.WriteLine(status.GetAllErrors());
        status.HasErrors.ShouldEqual(hasErrors);
    }
}