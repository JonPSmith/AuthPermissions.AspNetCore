// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.PermissionsCode
{
    public static class PermissionConstants
    {
        public const string PackedPermissionClaimType = "Permissions";
        public const string DayaKeyClaimType = "DataKey";
        public const char PackedAccessAllPermission = (char) ushort.MaxValue;

        public const string MigrationsHistoryTableName = nameof(AuthPermissions);
    }
}