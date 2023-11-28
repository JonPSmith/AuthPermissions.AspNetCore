// Copyright (c) 2023 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.BaseCode.PermissionsCode
{
    /// <summary>
    /// Holds a extension method to unpack permissions
    /// </summary>
    public static class PermissionUnpacker
    {
        /// <summary>
        /// This takes a string containing packed permissions and returns the names of the Permission member names
        /// </summary>
        /// <param name="packedPermissions"></param>
        /// <param name="permissionsEnumType"></param>
        /// <returns></returns>
        public static List<string> ConvertPackedPermissionToNames(this string packedPermissions, Type permissionsEnumType)
        {
            if (packedPermissions == null)
                return null;

            var permissionNames = new List<string>();
            foreach (var permissionChar in packedPermissions)
            {
                var enumName = Enum.GetName(permissionsEnumType, (ushort)permissionChar);
                if (enumName != null)
                    permissionNames.Add(enumName);
            }

            return permissionNames;
        }
    }
}