// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode.Internal;
using AuthPermissions.SetupCode;
using Xunit.Extensions.AssertExtensions;

namespace Test.TestHelpers
{
    public static class SetupHelpers
    {
        public static void SetupRolesInDb(this AuthPermissionsDbContext context, string lines = null)
        {
            lines ??= @"Role1 : One
Role2 |my description|: Two
Role3: Three";

            var service = new BulkLoadRolesService(context);
            var status = service.AddRolesToDatabaseIfEmpty(lines, typeof(TestEnum));
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.SaveChanges();
        }

        public static void AddUserToRoleInDb(this AuthPermissionsDbContext context, int numAdded, string userId = "User1")
        {
            var rolesInOrder = context.RoleToPermissions.OrderBy(x => x.RoleName).ToList();

            for (int i = 0; i < numAdded; i++)
            {
                context.Add(new UserToRole(userId, userId, rolesInOrder[i]));
            }
            context.SaveChanges();
        }

        public static void SetupTenantsInDb(this AuthPermissionsDbContext context)
        {
            var t1 = new Tenant("Tenant1");
            var t2 = new Tenant("Tenant2");
            var t3 = new Tenant("Tenant3");
            context.AddRange(t1,t2,t3);
            context.SaveChanges();
        }

        public static List<DefineUserWithRolesTenant> TestUserDefineWithUserId(string user2Roles = "Role1,Role2")
        {
            return new List<DefineUserWithRolesTenant>
            {
                new DefineUserWithRolesTenant("User1", "Role1", "1"),
                new DefineUserWithRolesTenant("User2", user2Roles, "2"),
                new DefineUserWithRolesTenant("User3", "Role1,Role3", "3"),
            };
        }

        public static List<DefineUserWithRolesTenant> TestUserDefineNoUserId(string user2Id = "User2")
        {
            return new List<DefineUserWithRolesTenant>
            {
                new DefineUserWithRolesTenant("User1", "Role1", "1"),
                new DefineUserWithRolesTenant("User2", "Role1,Role2", user2Id),
                new DefineUserWithRolesTenant("User3", "Role1,Role3", "3"),
            };
        }        
        
        public static List<DefineUserWithRolesTenant> TestUserDefineWithSuperUser(string user2Id = "User2")
        {
            return new List<DefineUserWithRolesTenant>
            {
                new DefineUserWithRolesTenant("User1", "Role1", "1"),
                new DefineUserWithRolesTenant("Super@g1.com", "Role1,Role2", null),
                new DefineUserWithRolesTenant("User3", "Role1,Role3", "3"),
            };
        }

        public static List<DefineUserWithRolesTenant> TestUserDefineWithTenants(string secondTenant = "Tenant2")
        {
            return new List<DefineUserWithRolesTenant>
            {
                new DefineUserWithRolesTenant("User1", "Role1", "1", null, "Tenant1"),
                new DefineUserWithRolesTenant("User2", "Role1,Role2", "2", null, secondTenant),
                new DefineUserWithRolesTenant("User3", "Role1,Role3", "3")
            };
        }
    }
}