// Copyright (c) 2021 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace AuthPermissions.BaseCode.CommonCode
{
    /// <summary>
    /// A AuthPermissions for internal errors
    /// </summary>
    public class AuthPermissionsException : Exception
    {
        /// <summary>
        /// Must contain a message
        /// </summary>
        /// <param name="message"></param>
        public AuthPermissionsException(string message)
            : base(message)
        {}
    }
}