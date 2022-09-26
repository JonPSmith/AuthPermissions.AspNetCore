// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using AuthPermissions.BaseCode.SetupCode;

namespace Example2.WebApiWithToken.IndividualAccounts.PermissionsCode
{
    public static class AppAuthSetupData
    {
        public static readonly List<BulkLoadRolesDto> RolesDefinition = new List<BulkLoadRolesDto>()
        {
            new("Role1", null, "Permission1"),
            new("Role2", null, "Permission1, Permission2"),
            new("Role3", null, "Permission1, Permission2, Permission3"),
            new("SuperRole", null, "AccessAll"),
        };

        public static readonly List<BulkLoadUserWithRolesTenant> UsersRolesDefinition = new List<BulkLoadUserWithRolesTenant>
        {
            new ("NoP@g1.com", null, ""),
            new ("P1@g1.com", null, "Role1"),
            new ("P2@g1.com", null, "Role1, Role2"),
            new ("P3@g1.com", null, "Role1, Role2, Role3"),
            new ("Super@g1.com", null, "SuperRole"),

        };
    }
}