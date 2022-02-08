// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.CommonCode;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.Classes.SupportTypes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupCode;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestClaimsCalculatorWithTenant
    {
        [Fact]
        public async Task TestCalcAllowedPermissionsNormalRolesTenantUser()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var setupUser = new SetupUserWithRoles(context, RoleTypes.Normal, true);

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions{ TenantType =  TenantTypes.SingleLevel }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(2);
            claims.First().Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            new string(claims.First().Value.OrderBy(x => x).ToArray()).ShouldEqual($"{(char)1}{(char)2}{(char)3}");
        }

        [Fact]
        public async Task TestCalcAllowedPermissionsWithTenantRoleTenantAutoAdd()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var setupUser = new SetupUserWithRoles(context, RoleTypes.TenantAutoAdd, true);

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions{ TenantType =  TenantTypes.SingleLevel }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(2);
            claims.First().Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            new string(claims.First().Value.OrderBy(x => x).ToArray()).ShouldEqual($"{(char)1}{(char)2}{(char)3}");
        }

        [Fact]
        public async Task TestCalcAllowedPermissionsWithTenantRoleTenantAdminAdd()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var setupUser = new SetupUserWithRoles(context, RoleTypes.TenantAdminAdd, true);

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(2);
            claims.First().Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            new string(claims.First().Value.OrderBy(x => x).ToArray()).ShouldEqual($"{(char)1}{(char)3}");
        }

        [Fact]
        public async Task TestCalcAllowedPermissionsNoUser()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions{ TenantType =  TenantTypes.SingleLevel }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(0);
        }

        [Fact]
        public async Task TestCalcDataKeySimple()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenant = Tenant.CreateSingleTenant("Tenant1").Result
                         ?? throw new AuthPermissionsException("CreateSingleTenant had errors.");
            var role = new RoleToPermissions("Role1", null, $"{((char) 1)}");
            var user = AuthUser.CreateAuthUser("User1", "User1@g.com", null, new List<RoleToPermissions>() { role }, tenant).Result;

            context.AddRange(tenant, role, user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions{ TenantType =  TenantTypes.SingleLevel }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(2);
            claims.First().Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            claims.Last().Type.ShouldEqual(PermissionConstants.DataKeyClaimType);
            claims.Last().Value.ShouldEqual(tenant.GetTenantDataKey());
        }

    }
}