// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using AuthPermissions.BaseCode.CommonCode;


namespace AuthPermissions.BaseCode.PermissionsCode
{
    /// <summary>
    /// This class contains extension methods to pack Permissions names into a unicode string
    /// </summary>
    public static class PermissionPacker
    {
        /// <summary>
        /// Packs permission names found in the comma delimited string into a unicode string
        /// </summary>
        /// <param name="enumPermissionsType"></param>
        /// <param name="permissionNames"></param>
        /// <returns></returns>
        public static string PackCommaDelimitedPermissionsNames(this Type enumPermissionsType, string permissionNames)
        {
            return enumPermissionsType.PackPermissionsNames(permissionNames.Split(',').Select(x => x.Trim()));
        }

        /// <summary>
        /// Packs a list of permissions names into a unicode string
        /// </summary>
        /// <param name="enumPermissionsType"></param>
        /// <param name="permissionNames"></param>
        /// <returns></returns>
        public static string PackPermissionsNames(this Type enumPermissionsType, IEnumerable<string> permissionNames)
        {
            var packedPermissions = permissionNames.Aggregate("", (s, permissionName) =>
                s + (char)Convert.ChangeType(Enum.Parse(enumPermissionsType, permissionName), typeof(char)));
            CheckPackedPermissionsDoesNotContainZeroChar(packedPermissions);
            return packedPermissions;
        }

        /// <summary>
        /// This returns true if the two packed Permissions are the same in length and content
        /// NOTE: It sorts the characters of two strings so that we only return true if they have the same permissions.
        /// </summary>
        /// <param name="packed1"></param>
        /// <param name="packed2"></param>
        /// <returns>true if the two packed Permissions are the same</returns>
        public static bool ComparesPackPermissions(this string packed1, string packed2)
        {
            if (packed1.Length != packed2.Length)
                return false;

            //Thanks to https://stackoverflow.com/a/6441603/1434764
            string SortString(string input)
            {
                char[] characters = input.ToArray();
                Array.Sort(characters);
                return new string(characters);
            }

            return SortString(packed1) == SortString(packed2);
        }

        /// <summary>
        /// This converts a list of enum permission names into a packed string. If any permission names are bad it calls the reportError action
        /// </summary>
        /// <param name="enumPermissionsType"></param>
        /// <param name="permissionNames"></param>
        /// <param name="reportError">Report a permission name that isn't in the list of enum members</param>
        /// <param name="foundAdvancedPermission">Only called if an advanced permission is found</param>
        /// <returns>the packed permission string</returns>
        public static string PackPermissionsNamesWithValidation(this Type enumPermissionsType,
            IEnumerable<string> permissionNames, Action<string> reportError, Action foundAdvancedPermission)
        {
            var packedPermissions = "";
            foreach (var permissionName in permissionNames)
            {
                try
                {
                    Enum.Parse(enumPermissionsType, permissionName);
                    var displayAttribute =  enumPermissionsType.GetMember(permissionName)[0].GetCustomAttribute<DisplayAttribute>();
                    if (displayAttribute?.GetAutoGenerateFilter() == true)
                        foundAdvancedPermission();
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