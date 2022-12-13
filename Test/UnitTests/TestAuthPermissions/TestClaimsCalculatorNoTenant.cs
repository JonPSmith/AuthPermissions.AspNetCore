// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using AuthPermissions.BaseCode.PermissionsCode;
using AuthPermissions.BaseCode.SetupCode;
using ExamplesCommonCode.IdentityCookieCode;
using Test.StubClasses;
using Test.TestHelpers;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestClaimsCalculatorNoTenant
    {
        [Fact]
        public async Task TestCalcAllowedPermissions()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            context.AddOneUserWithRolesAndOptionalTenant();
            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions{ TenantType =  TenantTypes.NotUsingTenants }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(1);
            claims.Single().Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            claims.Single().Value.ShouldEqual($"{(char)1}{(char)3}");
        }

        [Fact]
        public async Task TestCalcAddedClaims()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var user = AuthPSetupHelpers.CreateTestAuthUserOk("User1", "User1@g.com", null);
            context.Add(user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions { TenantType = TenantTypes.NotUsingTenants }, 
                new List<IClaimsAdder> { new AddRefreshEveryMinuteClaim() });

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(1);
            claims.Single().Type.ShouldEqual(PeriodicCookieEvent.TimeToRefreshUserClaimType);
            var rawString = claims.Single().Value;
            claims.GetClaimDateTimeTicksValue(PeriodicCookieEvent.TimeToRefreshUserClaimType)
                .ShouldBeInRange(DateTime.UtcNow.AddSeconds(59), DateTime.UtcNow.AddSeconds(61));
        }

        [Fact]
        public async Task TestCalcAllowedPermissionsOverlap()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var rolePer1 = new RoleToPermissions("Role1", null, $"{(char) 1}{(char) 3}");
            var rolePer2 = new RoleToPermissions("Role2", null, $"{(char)2}{(char)3}");
            context.AddRange(rolePer1, rolePer2);
            var user = AuthPSetupHelpers.CreateTestAuthUserOk("User1", "User1@g.com", null, 
                new List<RoleToPermissions>() { rolePer1, rolePer2 });
            context.Add(user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions{ TenantType =  TenantTypes.NotUsingTenants }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(1);
            claims.Single().Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            new string(claims.Single().Value.OrderBy(x => x).ToArray()).ShouldEqual($"{(char)1}{(char)2}{(char)3}");
        }

        [Fact]
        public async Task TestCalcAllowedPermissionsNoUser()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions{ TenantType =  TenantTypes.NotUsingTenants }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(0);
        }

        [Fact]
        public async Task TestUserIsDisabled()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var rolePer1 = new RoleToPermissions("Role1", null, $"{(char)1}{(char)3}");
            var rolePer2 = new RoleToPermissions("Role2", null, $"{(char)2}{(char)3}");
            context.AddRange(rolePer1, rolePer2);
            var user = AuthPSetupHelpers.CreateTestAuthUserOk("User1", "User1@g.com", null, 
                new List<RoleToPermissions>() { rolePer1 });
            user.UpdateIsDisabled(true);
            context.Add(user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions { TenantType = TenantTypes.NotUsingTenants }, new List<IClaimsAdder>());

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(0);
        }
    }
}