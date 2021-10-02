// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode;
using AuthPermissions.SetupCode;
using TestSupport.EfHelpers;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.TestAuthPermissions
{
    public class TestClaimsCalculator
    {
        [Fact]
        public async Task TestCalcAllowedPermissionsNoTenant()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var rolePer1 = new RoleToPermissions("Role1", null,
                $"{(char)1}{(char)3}");
            var rolePer2 = new RoleToPermissions("Role2", null,
                $"{(char)2}{(char)3}");
            context.AddRange(rolePer1, rolePer2);
            var user = new AuthUser("User1", "User1@g.com", null, new[] { rolePer1 });
            context.Add(user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions{ TenantType =  TenantTypes.NotUsingTenants });

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(1);
            claims.Single().Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            claims.Single().Value.ShouldEqual($"{(char)1}{(char)3}");
        }

        [Fact]
        public async Task TestCalcAllowedPermissionsOverlap()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var rolePer1 = new RoleToPermissions("Role1", null,
                $"{(char) 1}{(char) 3}");
            var rolePer2 = new RoleToPermissions("Role2", null,
                $"{(char)2}{(char)3}");
            context.AddRange(rolePer1, rolePer2);
            var user = new AuthUser("User1", "User1@g.com", null, new[] { rolePer1, rolePer2 });
            context.Add(user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions { TenantType = TenantTypes.NotUsingTenants });

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

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions { TenantType = TenantTypes.NotUsingTenants });

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

            var tenant = new Tenant("Tenant1");
            var role = new RoleToPermissions("Role1", null, $"{((char) 1)}");
            var user = new AuthUser("User1", "User1@g.com", null, new [] {role}, tenant);
            context.AddRange(tenant, role, user);
            context.SaveChanges();

            context.ChangeTracker.Clear();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("User1");

            //VERIFY
            claims.Count.ShouldEqual(2);
            claims.First().Type.ShouldEqual(PermissionConstants.PackedPermissionClaimType);
            claims.Last().Type.ShouldEqual(PermissionConstants.DataKeyClaimType);
            claims.Last().Value.ShouldEqual(tenant.GetTenantDataKey());
        }

        [Fact]
        public async Task TestCalcDataKeyNoUser()
        {
            //SETUP
            var options = SqliteInMemory.CreateOptions<AuthPermissionsDbContext>();
            using var context = new AuthPermissionsDbContext(options);
            context.Database.EnsureCreated();

            var service = new ClaimsCalculator(context, new AuthPermissionsOptions { TenantType = TenantTypes.SingleLevel });

            //ATTEMPT
            var claims = await service.GetClaimsForAuthUserAsync("NoUser");

            //VERIFY
            claims.Count.ShouldEqual(0);
        }

    }
}