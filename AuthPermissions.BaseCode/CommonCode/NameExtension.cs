// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.BaseCode.CommonCode
{
    /// <summary>
    /// Simple extension method to improve error feedback in ASP.NET Core
    /// </summary>
    public static class NameExtension
    {

        /// <summary>
        /// This converts a camel-cased string to Pascal-case
        /// This is used to convert method names into the name used in a class
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string CamelToPascal(this string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            return char.ToUpper(name[0]) + name.Substring(1);
        }
    }
}