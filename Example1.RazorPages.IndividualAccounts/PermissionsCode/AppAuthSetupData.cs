// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.BaseCode.SetupCode;

namespace Example1.RazorPages.IndividualAccounts.PermissionsCode
{
    public static class AppAuthSetupData
    {
        public static readonly List<BulkLoadRolesDto> RolesDefinition = new()
        {
            new("Role1", "Staff Role", "Permission1"),
            new("Role2",  "Manager Role", "Permission1, Permission2"),
            new("SuperAdmin", "Super admin - only use for setup", "AccessAll"),
        };

        public static readonly List<BulkLoadUserWithRolesTenant> UsersWithRolesDefinition = new()
        {
            new ("Staff@g1.com", null, "Role1"),
            new ("Manager@g1.com", null, "Role2"),
            new ( "Super@g1.com", null, "SuperAdmin"),
        };
    }
}