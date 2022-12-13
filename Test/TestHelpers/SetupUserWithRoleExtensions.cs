// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.BaseCode;
using AuthPermissions.BaseCode.CommonCode;
using AuthPermissions.BaseCode.DataLayer.Classes;
using AuthPermissions.BaseCode.DataLayer.Classes.SupportTypes;
using AuthPermissions.BaseCode.DataLayer.EfCode;
using Test.StubClasses;
using Xunit.Extensions.AssertExtensions;

namespace Test.TestHelpers;

public static class SetupUserWithRoleExtensions
{

    /// <summary>
    /// This creates a new user with two roles
    /// </summary>
    /// <param name="context"></param>
    /// <param name="role2Type">Role2's type and added to user</param>
    /// <param name="userHasTenant">If true then tenant created and user linked to it</param>
    public static AuthUser SetupUserWithDifferentRoleTypes(this AuthPermissionsDbContext context, RoleTypes role2Type, bool userHasTenant)
    {
        var rolePer1 = new RoleToPermissions("Role1", null, $"{(char)1}{(char)3}");
        var rolePer2 = new RoleToPermissions("Role2", null, $"{(char)2}{(char)3}", role2Type);

        var rolesForTenant = role2Type == RoleTypes.TenantAutoAdd || role2Type == RoleTypes.TenantAdminAdd
            ? new List<RoleToPermissions> { rolePer2 }
            : new List<RoleToPermissions>();

        var tenant = userHasTenant ? AuthPSetupHelpers.CreateTestSingleTenantOk("Tenant1", rolesForTenant)
            : null;

        context.AddRange(rolePer1, rolePer2);
        var rolesForUsers = role2Type == RoleTypes.Normal || role2Type == RoleTypes.HiddenFromTenant
            ? new List<RoleToPermissions>() { rolePer1, rolePer2 }
            : new List<RoleToPermissions>() { rolePer1 };

        var authUser = AuthPSetupHelpers.CreateTestAuthUserOk("User1", "User1@g.com", null, rolesForUsers, tenant);

        context.Add(authUser);
        context.SaveChanges();

        return authUser;
    }
}