// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;

namespace AuthPermissions.PermissionsCode
{
    public static class PermissionConstants
    {
        public const string PackedPermissionClaimType = "Permissions";
        public const char PackedAccessAllPermission = (char) Int16.MaxValue;

        public const string MigrationsHistoryTableName = nameof(AuthPermissions);
    }
}