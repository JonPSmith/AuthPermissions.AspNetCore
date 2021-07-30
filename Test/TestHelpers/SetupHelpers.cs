// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthPermissions;
using AuthPermissions.AspNetCore.JwtTokenCode;
using AuthPermissions.BulkLoadServices.Concrete;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.SetupCode;
using Xunit.Extensions.AssertExtensions;

namespace Test.TestHelpers
{
    public static class SetupHelpers
    {
        public static AuthPJwtConfiguration CreateTestJwtSetupData(TimeSpan expiresIn = default)
        {

            var data = new AuthPJwtConfiguration
            {
                Issuer = "issuer",
                Audience = "audience",
                SigningKey = "long-key-with-lots-of-data-in-it",
                TokenExpires = expiresIn == default ? new TimeSpan(0, 0, 50) : expiresIn,
                RefreshTokenExpires = expiresIn == default ? new TimeSpan(0, 0, 50) : expiresIn,
            };

            return data;
        }


        public static async Task SetupRolesInDbAsync(this AuthPermissionsDbContext context, string lines = null)
        {
            lines ??= @"Role1 : One
Role2 |my description|: Two
Role3: Three";

            var authOptions = new AuthPermissionsOptions();
            authOptions.InternalData.EnumPermissionsType = typeof(TestEnum);
            var service = new BulkLoadRolesService(context, authOptions);
            var status = await service.AddRolesToDatabaseAsync(lines);
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.SaveChanges();
        }

        public static AuthUser AddUserWithRoleInDb(this AuthPermissionsDbContext context, string userId = "User1")
        {
            var user = new AuthUser(userId, userId, null, context.RoleToPermissions.OrderBy(x => x.RoleName));
            context.Add(user);
            context.SaveChanges();
            return user;
        }

        /// <summary>
        /// This adds AuthUser with an ever increasing number of roles
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userIdCommaDelimited"></param>
        public static void AddMultipleUsersWithRolesInDb(this AuthPermissionsDbContext context, string userIdCommaDelimited = "User1,User2,User3")
        {
            var rolesInDb = context.RoleToPermissions.OrderBy(x => x.RoleName).ToList();
            var userIds = userIdCommaDelimited.Split(',');
            for (int i = 0; i < userIds.Length; i++)
            {
                var user = new AuthUser(userIds[i], $"{userIds[i]}@gmail.com", $"first last {i}", rolesInDb.Take(i+1));
                context.Add(user);
            }
            context.SaveChanges();
        }

        public static void SetupSingleTenantsInDb(this AuthPermissionsDbContext context)
        {
            var t1 = new Tenant("Tenant1");
            var t2 = new Tenant("Tenant2");
            var t3 = new Tenant("Tenant3");
            context.AddRange(t1,t2,t3);
            context.SaveChanges();
        }

        public static async Task<List<string>> SetupHierarchicalTenantInDb(this AuthPermissionsDbContext context)
        {
            var service = new BulkLoadTenantsService(context);
            var authOptions = new AuthPermissionsOptions {TenantType = TenantTypes.HierarchicalTenant};
            var lines = @"Company
Company | West Coast 
Company | West Coast | SanFran
Company | West Coast | SanFran | Shop1
Company | West Coast | SanFran | Shop2
Company | East Coast
Company | East Coast | New York 
Company | East Coast | New York | Shop3
Company | East Coast | New York | Shop4";

            (await service.AddTenantsToDatabaseAsync(lines, authOptions)).IsValid.ShouldBeTrue();

            return lines.Split(Environment.NewLine).ToList();
        }

        public static List<DefineUserWithRolesTenant> TestUserDefineWithUserId(string user2Roles = "Role1,Role2")
        {
            return new List<DefineUserWithRolesTenant>
            {
                new DefineUserWithRolesTenant("User1", null, "Role1", userId: "1"),
                new DefineUserWithRolesTenant("User2", null, user2Roles, userId: "2"),
                new DefineUserWithRolesTenant("User3", null, "Role1,Role3", userId: "3"),
            };
        }

        public static List<DefineUserWithRolesTenant> TestUserDefineNoUserId(string user2Id = "User2")
        {
            return new List<DefineUserWithRolesTenant>
            {
                new DefineUserWithRolesTenant("User1", null, "Role1", userId: "1"),
                new DefineUserWithRolesTenant("User2", null, "Role1,Role2", userId: user2Id),
                new DefineUserWithRolesTenant("User3", null, "Role1,Role3", userId: "3"),
            };
        }        
        
        public static List<DefineUserWithRolesTenant> TestUserDefineWithSuperUser(string user2Id = "User2")
        {
            return new List<DefineUserWithRolesTenant>
            {
                new DefineUserWithRolesTenant("User1", null, "Role1", userId: "1"),
                new DefineUserWithRolesTenant("Super@g1.com",null,  "Role1,Role2", userId: null),
                new DefineUserWithRolesTenant("User3", null, "Role1,Role3", userId: "3"),
            };
        }

        public static List<DefineUserWithRolesTenant> TestUserDefineWithTenants(string secondTenant = "Tenant2")
        {
            return new List<DefineUserWithRolesTenant>
            {
                new DefineUserWithRolesTenant("User1", null, "Role1", userId: "1", uniqueUserName: null, tenantNameForDataKey: "Tenant1"),
                new DefineUserWithRolesTenant("User2", null, "Role1,Role2", userId: "2", uniqueUserName: null, tenantNameForDataKey: secondTenant),
                new DefineUserWithRolesTenant("User3", null, "Role1,Role3", userId: "3", uniqueUserName: null, tenantNameForDataKey: "Tenant3")
            };
        }
    }
}