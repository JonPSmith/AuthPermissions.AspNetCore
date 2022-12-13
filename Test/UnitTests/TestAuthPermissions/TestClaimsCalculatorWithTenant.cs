// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AdminCode;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.BaseCode.SetupCode;
using AuthPermissions.SetupCode;
using Test.StubClasses;
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

            var setupUser = context.SetupUserWithDifferentRoleTypes(RoleTypes.Normal, true);

            context.ChangeTracker.Clear();

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

            var setupUser = context.SetupUserWithDifferentRoleTypes(RoleTypes.TenantAutoAdd, true);

            context.ChangeTracker.Clear();

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

            var setupUser = context.SetupUserWithDifferentRoleTypes(RoleTypes.TenantAdminAdd, true);

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

            context.ChangeTracker.Clear();

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

            var tenant = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1");
            var role = new RoleToPermissions("Role1", null, $"{((char) 1)}");
            var user = AuthPSetupHelpers.CreateTestAuthUserOk("User1", "User1@g.com", null, 
                new List<RoleToPermissions>() { role }, tenant);

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

        [Fact]
        public async Task TestCalcDataKeySharding()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenant = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1");
            tenant.UpdateShardingState("MyConnectionName", false);
            var role = new RoleToPermissions("Role1", null, $"{((char)1)}");
            var user = AuthPSetupHelpers.CreateTestAuthUserOk("User1", "User1@g.com", null, 
                new List<RoleToPermissions>() { role }, tenant);

            context.AddRange(tenant, role, user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, 
                new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel | TenantTypes.AddSharding }, 
                new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(3);
            claims[0].Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            claims[1].Type.ShouldEqual(PermissionConstants.DataKeyClaimType);
            claims[1].Value.ShouldEqual(tenant.GetTenantDataKey());
            claims[2].Type.ShouldEqual(PermissionConstants.DatabaseInfoNameType);
            claims[2].Value.ShouldEqual(tenant.DatabaseInfoName);
        }

        [Fact]
        public async Task TestCalcDataKeyShardingNoQueryFilter()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenant = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1");
            tenant.UpdateShardingState("MyConnectionName", true);
            var role = new RoleToPermissions("Role1", null, $"{((char)1)}");
            var user = AuthPSetupHelpers.CreateTestAuthUserOk("User1", "User1@g.com", null, 
                new List<RoleToPermissions>() { role }, tenant);

            context.AddRange(tenant, role, user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context,
                new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel | TenantTypes.AddSharding },
                new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(3);
            claims[0].Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            claims[1].Type.ShouldEqual(PermissionConstants.DataKeyClaimType);
            claims[1].Value.ShouldEqual(MultiTenantExtensions.DataKeyNoQueryFilter);
            claims[2].Type.ShouldEqual(PermissionConstants.DatabaseInfoNameType);
            claims[2].Value.ShouldEqual(tenant.DatabaseInfoName);
        }

        [Fact]
        public async Task TestUserIsDisabled()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var tenant = AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1");
            var role = new RoleToPermissions("Role1", null, $"{((char)1)}");
            var user = AuthPSetupHelpers.CreateTestAuthUserOk("User1", "User1@g.com", null,
                new List<RoleToPermissions> { role }, tenant);
            user.UpdateIsDisabled(true);

            context.AddRange(tenant, role, user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(0);
        }
    }
}