// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AuthPermissions.CommonCode;

[assembly: InternalsVisibleTo("Test")]
namespace AuthPermissions.PermissionsCode.Internal
{
    internal static class PermissionPacker
    {
        //private readonly Dictionary<char, string> _enumLookup;

        //public PermissionHandler(Type enumType)
        //{
        //    enumType.ThrowExceptionIfEnumIsNotCorrect();

        //    _enumLookup = Enum.GetNames(enumType)
        //        .ToDictionary(key => (char)((short)Enum.Parse(enumType, key)), value => value);
        //}

        public static string PackCommaDelimitedPermissionsNames(this Type enumPermissionsType, string permissionNames)
        {
            return enumPermissionsType.PackPermissionsNames(permissionNames.Split(',').Select(x => x.Trim()));
        }

        public static string PackPermissionsNames(this Type enumPermissionsType, IEnumerable<string> permissionNames)
        {
            return permissionNames.Aggregate("", (s, permissionName) =>
                s + (char)Convert.ChangeType(Enum.Parse(enumPermissionsType, permissionName), typeof(char)));
        }

        /// <summary>
        /// This checks a permissionName is valid for the enumPermissionsType
        /// </summary>
        /// <param name="enumPermissionsType"></param>
        /// <param name="permissionName"></param>
        /// <returns>true if valid</returns>
        public static bool PermissionsNameIsValid(this Type enumPermissionsType, string permissionName)
        {
            try
            {
                Enum.Parse(enumPermissionsType, permissionName);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }
    }
}