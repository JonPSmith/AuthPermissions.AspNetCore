// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AuthPermissions.CommonCode;
using StatusGeneric;

[assembly: InternalsVisibleTo("Test")]
namespace AuthPermissions.PermissionsCode.Internal
{
    internal static class PermissionPacker
    {
        public static string PackCommaDelimitedPermissionsNames(this Type enumPermissionsType, string permissionNames)
        {
            return enumPermissionsType.PackPermissionsNames(permissionNames.Split(',').Select(x => x.Trim()));
        }

        public static string PackPermissionsNames(this Type enumPermissionsType, IEnumerable<string> permissionNames)
        {
            var packedPermissions = permissionNames.Aggregate("", (s, permissionName) =>
                s + (char)Convert.ChangeType(Enum.Parse(enumPermissionsType, permissionName), typeof(char)));
            CheckPackedPermissionsDoesNotContainZeroChar(packedPermissions);
            return packedPermissions;
        }

        /// <summary>
        /// This converts a list of enum permission names into a packed string. If any permission names are bad it calls the reportError action
        /// </summary>
        /// <param name="enumPermissionsType"></param>
        /// <param name="permissionNames"></param>
        /// <param name="reportError"></param>
        /// <returns>the packed permission string</returns>
        public static string PackPermissionsNamesWithValidation(this Type enumPermissionsType,
            IEnumerable<string> permissionNames, Action<string> reportError)
        {
            var packedPermissions = "";
            foreach (var permissionName in permissionNames)
            {
                try
                {
                    Enum.Parse(enumPermissionsType, permissionName);
                }
                catch (ArgumentException)
                {
                    reportError(permissionName);
                    continue;
                }

                packedPermissions +=
                    (char) Convert.ChangeType(Enum.Parse(enumPermissionsType, permissionName), typeof(char));
            }
            CheckPackedPermissionsDoesNotContainZeroChar(packedPermissions);
            return packedPermissions;
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

        //----------------------------------------------------------------------
        // private methods

        private static void CheckPackedPermissionsDoesNotContainZeroChar(string packedPermissions)
        {
            if (packedPermissions.Contains((char)0))
                throw new AuthPermissionsBadDataException(
                    "A packed permissions string must not contain a char of zero value");
        }
    }
}