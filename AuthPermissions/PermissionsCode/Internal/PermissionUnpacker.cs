// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace AuthPermissions.PermissionsCode.Internal
{
    internal static class PermissionUnpacker
    {
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