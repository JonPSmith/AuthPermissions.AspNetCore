// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.DataLayer.Classes;
using AuthPermissions.DataLayer.EfCode;
using AuthPermissions.PermissionsCode.Internal;
using AuthPermissions.SetupParts;
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

            var service = new SetupRolesService(context);
            var status = service.AddRolesToDatabaseIfEmpty(lines, typeof(TestEnum));
            status.IsValid.ShouldBeTrue(status.GetAllErrors());
            context.SaveChanges();
        }

        public static List<DefineUserWithRolesTenant> TestUserDefine(string user2Roles = "Role1,Role2")
        {
            return new List<DefineUserWithRolesTenant>
            {
                new DefineUserWithRolesTenant("1", "User1", "Role1"),
                new DefineUserWithRolesTenant("2", "User2", user2Roles),
                new DefineUserWithRolesTenant("3", "User3", "Role1,Role3"),
            };
        }
    }
}