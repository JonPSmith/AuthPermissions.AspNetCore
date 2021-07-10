// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using StatusGeneric;

namespace AuthPermissions.CommonCode
{
    /// <summary>
    /// Various error reporting extensions for the AuthP code
    /// </summary>
    public static class ErrorReportingExtensions
    {
        /// <summary>
        /// Checks the permission type is correct
        /// </summary>
        /// <param name="permissionType"></param>
        public static void ThrowExceptionIfEnumIsNotCorrect(this Type permissionType)
        {
            if (!permissionType.IsEnum)
                throw new AuthPermissionsException("The permissions must be an enum");
            if (Enum.GetUnderlyingType(permissionType) != typeof(ushort))
                throw new AuthPermissionsException(
                    $"The enum permissions {permissionType.Name} should by 16 bits in size to work.\n" +
                    $"Please add ': ushort' to your permissions declaration, i.e. public enum {permissionType.Name} : ushort " + "{...};");
        }


        /// <summary>
        /// This throws an AuthPermissionsBadDataException 
        /// </summary>
        /// <param name="status"></param>
        public static void IfErrorsTurnToException(this IStatusGeneric status)
        {
            if (status.HasErrors)
                throw new AuthPermissionsBadDataException(status.Errors.Count() == 1
                    ? status.Errors.Single().ToString()
                    : $"{status.HasErrors}:{Environment.NewLine}{status.GetAllErrors()}");
        }

        /// <summary>
        /// This forms an error with the line of data has has an error, with an optional pointer to the char that had the problem
        /// </summary>
        /// <param name="line">line of input text that has a problem</param>
        /// <param name="lineNum"></param>
        /// <param name="charNum">If not negative it outputs another line below the bad line of text pointing to the point where the error was found</param>
        /// <param name="error">The error message</param>
        /// <returns></returns>
        public static string FormErrorString(this string line, int lineNum, int charNum, string error)
        {
            var charPart = charNum < 0 ? "" : $", char: {charNum + 1}";
            var result = $"Line/index {lineNum + 1:####}{charPart}: {error}{Environment.NewLine}{line}";
            if (charNum > -1)
                result += Environment.NewLine + new string(' ', charNum) + "|";

            return result;
        }
    }
}