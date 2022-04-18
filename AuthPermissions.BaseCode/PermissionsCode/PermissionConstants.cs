// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.BaseCode.PermissionsCode
{
    /// <summary>
    /// Various permission constants
    /// </summary>
    public static class PermissionConstants
    {
        /// <summary>
        /// The claim name holding the packed permission string
        /// </summary>
        public const string PackedPermissionClaimType = "Permissions";
        /// <summary>
        /// The claim name holding the optional DataKey
        /// </summary>
        public const string DataKeyClaimType = "DataKey";
        /// <summary>
        /// The claim name holding the name of the database data in the shardingsettings file
        /// </summary>
        public const string DatabaseInfoNameType = "DatabaseInfoName";

        /// <summary>
        /// This is the char for the AccessAll permission
        /// </summary>
        public const char PackedAccessAllPermission = (char) ushort.MaxValue;
    }
}